/*
 * ZXingCam - offline barcode decoding from live camera frames using zxing-wasm (ZXing-C++).
 * Requires: Scripts/barcode/zxing-reader.min.js (global ZXingWASM) and zxing_reader.wasm served from the same folder.
 *
 * Usage:
 *   ZXingCam.init('/Scripts/barcode/').then(function () { ... });
 *   ZXingCam.decodeRegion(videoEl, {x:0, y:0, w:1280, h:400}).then(function (text) { ... });
 */
(function (global) {
    'use strict';

    var _initPromise = null;
    var _wasmBase = '';
    var _workCanvas = null;
    var _logOnce = {};

    var DEFAULT_FORMATS = ['Code128', 'EAN13', 'EAN8', 'UPCA', 'UPCE', 'Code39', 'ITF', 'Codabar'];

    var BASE_OPTS = {
        formats: DEFAULT_FORMATS.slice(),
        tryHarder: true,
        tryRotate: true,
        tryInvert: true,
        tryDownscale: true,
        binarizer: 'LocalAverage',
        minLineCount: 2,
        maxNumberOfSymbols: 3,
        textMode: 'Plain'
    };

    function logOnce(key, arg) {
        if (_logOnce[key]) return;
        _logOnce[key] = true;
        console.warn('[ZXING]', arg);
    }

    function init(baseUrl) {
        if (baseUrl !== undefined && baseUrl !== null) _wasmBase = String(baseUrl);
        if (_initPromise) return _initPromise;
        _initPromise = new Promise(function (resolve, reject) {
            if (typeof global.ZXingWASM === 'undefined') {
                reject(new Error('zxing-reader library not loaded'));
                return;
            }
            try {
                global.ZXingWASM.setZXingModuleOverrides({
                    locateFile: function (file) {
                        return _wasmBase + file;
                    }
                });
            } catch (e) {
                logOnce('overrides', e);
            }
            // Warm up: instantiate the wasm module with a tiny dummy image.
            global.ZXingWASM.readBarcodes({ width: 2, height: 2, data: new Uint8Array(16) }, BASE_OPTS)
                .then(function () { resolve(true); })
                .catch(function (e) { reject(e); });
        });
        return _initPromise;
    }

    function grabRegion(video, region) {
        if (!_workCanvas) {
            _workCanvas = document.createElement('canvas');
        }
        _workCanvas.width = region.w;
        _workCanvas.height = region.h;
        var ctx = _workCanvas.getContext('2d', { willReadFrequently: true });
        ctx.drawImage(video, region.x, region.y, region.w, region.h, 0, 0, region.w, region.h);
        return ctx.getImageData(0, 0, region.w, region.h);
    }

    function stretchContrast(imageData) {
        var w = imageData.width, h = imageData.height;
        var src = imageData.data;
        var n = w * h;
        var lum = new Uint8Array(n);
        var i, o;
        for (i = 0; i < n; i++) {
            o = i << 2;
            lum[i] = (306 * src[o] + 601 * src[o + 1] + 117 * src[o + 2]) >> 10;
        }
        var hist = new Uint32Array(256);
        for (i = 0; i < n; i++) hist[lum[i]]++;
        var loTarget = n * 0.02, hiTarget = n * 0.02;
        var acc = 0, lo = 0, hi = 255;
        for (i = 0; i < 256; i++) { acc += hist[i]; if (acc >= loTarget) { lo = i; break; } }
        acc = 0;
        for (i = 255; i >= 0; i--) { acc += hist[i]; if (acc >= hiTarget) { hi = i; break; } }
        if (hi - lo < 8) { lo = 0; hi = 255; }
        var span = hi - lo;
        var out = new Uint8Array(n * 4);
        for (i = 0; i < n; i++) {
            var g = ((lum[i] - lo) * 255 / span) | 0;
            if (g < 0) g = 0; else if (g > 255) g = 255;
            o = i << 2;
            out[o] = out[o + 1] = out[o + 2] = g;
            out[o + 3] = 255;
        }
        return { width: w, height: h, data: out };
    }

    function upscale2x(imageData) {
        var w = imageData.width, h = imageData.height;
        var src = imageData.data;
        var W = w << 1, H = h << 1;
        var out = new Uint8Array(W * H * 4);
        var x, y, o, s;
        for (y = 0; y < h; y++) {
            for (x = 0; x < w; x++) {
                s = ((y * w + x) << 2);
                var r = src[s], g = src[s + 1], b = src[s + 2];
                var p0 = (((y << 1) * W + (x << 1)) << 2);
                var p1 = (p0 + 4);
                var p2 = (p0 + (W << 2));
                var p3 = (p2 + 4);
                out[p0] = out[p1] = out[p2] = out[p3] = r;
                out[p0 + 1] = out[p1 + 1] = out[p2 + 1] = out[p3 + 1] = g;
                out[p0 + 2] = out[p1 + 2] = out[p2 + 2] = out[p3 + 2] = b;
                out[p0 + 3] = out[p1 + 3] = out[p2 + 3] = out[p3 + 3] = 255;
            }
        }
        return { width: W, height: H, data: out };
    }

    function pick(results) {
        if (!results || !results.length) return null;
        var i, r;
        for (i = 0; i < results.length; i++) {
            r = results[i];
            if (r.text && /^\d{6,14}$/.test(r.text)) return r.text;
        }
        for (i = 0; i < results.length; i++) {
            if (results[i].text && results[i].text.trim()) return results[i].text;
        }
        return null;
    }

    function safeRead(img, opts) {
        return global.ZXingWASM.readBarcodes(img, opts).then(pick).catch(function (e) {
            logOnce('read', e);
            return null;
        });
    }

    // Decode one tick. Returns Promise<string|null>.
    function decodeRegion(video, region, tick) {
        if (!_initPromise) return Promise.reject(new Error('ZXingCam.init not called'));
        tick = tick || 0;
        return _initPromise.then(function () {
            var img;
            try {
                img = grabRegion(video, region);
            } catch (e) {
                logOnce('grab', e);
                return null;
            }
            return safeRead(img, BASE_OPTS).then(function (text) {
                if (text) return text;
                var st = stretchContrast(img);
                return safeRead(st, BASE_OPTS).then(function (text2) {
                    if (text2) return text2;
                    var ghOpts = {
                        formats: DEFAULT_FORMATS.slice(),
                        tryHarder: true,
                        tryRotate: true,
                        tryInvert: true,
                        tryDownscale: true,
                        binarizer: 'GlobalHistogram',
                        minLineCount: 2,
                        maxNumberOfSymbols: 3,
                        textMode: 'Plain'
                    };
                    return safeRead(img, ghOpts).then(function (text3) {
                        if (text3) return text3;
                        // Extra passes every 4th tick to keep frame rate up.
                        if (tick % 4 !== 0) return null;
                        var opts = {
                            formats: DEFAULT_FORMATS.slice(),
                            tryHarder: true,
                            tryRotate: true,
                            tryInvert: true,
                            tryDownscale: true,
                            tryDenoise: true,
                            binarizer: 'LocalAverage',
                            minLineCount: 2,
                            maxNumberOfSymbols: 3,
                            textMode: 'Plain'
                        };
                        return safeRead(upscale2x(st), opts);
                    });
                });
            });
        });
    }

    global.ZXingCam = {
        init: init,
        decodeRegion: decodeRegion,
        formats: DEFAULT_FORMATS
    };
})(this);
