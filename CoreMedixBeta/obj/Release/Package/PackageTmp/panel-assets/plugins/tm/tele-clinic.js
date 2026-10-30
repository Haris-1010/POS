'use strict';

var browserSupported = true;
var camerAudioPermissions = true;



var Video = require(['https://media.twiliocdn.com/sdk/js/video/v1/twilio-video.min.js']);
var activeRoom;
var previewTracks;
var roomName;
var token;
var roomGlobal;
var locationId;
let totalParticipants = 0;
var attrs = document.querySelector('script[data-token][data-room][data-locid]');
token = attrs.getAttribute('data-token');
roomName = attrs.getAttribute('data-room');
locationId = attrs.getAttribute('data-locid');
var fileData = new FormData();


$(document).ready(function () {

    $("#attachedFiles").on("change", function () {
        var val = $(this).val().toLowerCase(),
            regex = new RegExp("(.*?)\.(docx|doc|pdf|png|jpg|jpeg)$");

        if (!(regex.test(val))) {
            $(this).val('');

        }
        else {
            var fileInput = document.getElementById('attachedFiles');
            for (var i = 0; i < fileInput.files.length; i++) {
                var sfilename = fileInput.files[i].name;
                var srandomid = Math.random().toString(36).substring(7);
                fileData.append(srandomid, fileInput.files[i]);

                var html = "<tr id='" + srandomid + "'><td>" + sfilename + "</td><td><a href='#' onclick='DeleteFile(`" + srandomid + "`)'><span class='fa fa-times' style='color:red;'></span></a></td></tr>";
                $("#filesList").append(html);
                $("#uploadFiles").removeAttr("disabled");
            }
        }

    });

    //For IOS
    $(window).on('pagehide', function () {
        leaveRoomIfJoined();
    });

    $("#endcall").click(function () {
        $(this).find('i').addClass('fas fa-phone-slash');
        leaveRoomIfJoined();
    });

    $("#camera").click(function () {
        let camera_status = this.dataset.camera_status;
        if (camera_status === "true") {
            $(this).find('i').removeClass('fa-video');
            $(this).find('i').addClass('fa-video-slash');

            roomGlobal.localParticipant.videoTracks.forEach(function (videoTrack) {
                console.log("turning off local user video");
                videoTrack.enable(false);
            });
        }
        else {

            $(this).find('i').addClass('fa-video');
            $(this).find('i').removeClass('fa-video-slash');
            roomGlobal.localParticipant.videoTracks.forEach(function (videoTrack) {
                console.log("turning on local user video");
                videoTrack.enable(true);
            });
        }

        if (camera_status === "false") {
            camera_status = "true";
        }
        else {
            camera_status = "false";
        }
        this.dataset.camera_status = camera_status;

    });

    $("#myaudio").click(function () {

        let audio_status = this.dataset.audio_status;

        //if true then close the audio
        if (audio_status === "true") {

            $(this).find('i').removeClass('fa-microphone');
            $(this).find('i').addClass('fa-microphone-slash');

            roomGlobal.localParticipant.audioTracks.forEach(function (audioTrack) {
                console.log("muting local users audio");

                audioTrack.disable();
            });
        }
        else {
            $(this).find('i').removeClass('fa-microphone-slash');
            $(this).find('i').addClass('fa-microphone');
            roomGlobal.localParticipant.audioTracks.forEach(function (audioTrack) {
                console.log("turning on local users audio");

                audioTrack.enable();
            });
        }
        if (audio_status === "false") {
            audio_status = "true";
        }
        else {
            audio_status = "false";
        }
        this.dataset.audio_status = audio_status;
    });


    if (webSocketsSupport() !== true) {
        browserSupported = false;
    }

    if (navigator.mediaDevices === undefined) {
        browserSupported = false;
    }

    if (navigator.mediaDevices.getUserMedia === undefined) {
        navigator.mediaDevices.getUserMedia = function (constraints) {

            var getUserMedia = navigator.webkitGetUserMedia || navigator.mozGetUserMedia;

            if (!getUserMedia) {
                return Promise.reject(new Error('getUserMedia is not implemented in this browser'));
            }

            return new Promise(function (resolve, reject) {
                getUserMedia.call(navigator, constraints, resolve, reject);
            });
        };
    }

    navigator.mediaDevices.getUserMedia({ audio: true, video: true })
        .then(function (stream) {

            if (!browserSupported) {
                if ($('body').is(':loading')) {
                    $('body').loading('stop');
                }
                var html = '<p>It Seems You Are Using OutUpdated Browser.Please Update Your Browser and try Again</p>';
                $(".error-bar").html(html).show();
                $("#notifyModal").modal('show');
            }
            else {
                $("#notifyModal").modal('hide');
                $(".error-bar").hide();
                navigator.mediaDevices.enumerateDevices().then(function (devices) { getCameraDevices(devices, stream.getTracks()); });
                //connectcall();
            }
        })
        .catch(function (err) {
            console.log(err.name + ": " + err.message);
            if (err.name === "NotAllowedError") {
                camerAudioPermissions = false;
                if ($('body').is(':loading')) {
                    $('body').loading('stop');
                }
                var html = '<p>It Seems Permission For Camera/Mic Is Not Granted.Please Grant Permissions By Clicking <i class="fa fa-video"></i> Icon In Address Bar.</p>';

                $(".error-bar").html(html).show();
                $("#notifyModal").modal('show');
            }

            else if (err.name === "NotReadableError") {
                camerAudioPermissions = false;
                if ($('body').is(':loading')) {
                    $('body').loading('stop');
                }
                var html = '<p>It Seems Some Hardware Issue Occured At Your End.Please Check Your Camera And MicroPhone.</p>';

                $(".error-bar").html(html).show();
                $("#notifyModal").modal('show');
            }
            else if (err.name === "AbortError") {
                camerAudioPermissions = false;
                if ($('body').is(':loading')) {
                    $('body').loading('stop');
                }
                var html = '<p>It Seems Some Hardware Issue Occured At Your End.Please Check Your Camera And MicroPhone.</p>';

                $(".error-bar").html(html).show();
                $("#notifyModal").modal('show');
            }
        });


});


window.onbeforeunload = function () {
    leaveRoomIfJoined();
};


function attachTracks(tracks, container) {
    tracks.forEach(function (track) {
        container.appendChild(track.attach());
    });
}

function attachParticipantTracks(participant, container) {
    var tracks = Array.from(participant.tracks.values());
    attachTracks(tracks, container);
}

function detachTracks(tracks, VideoEl) {
    tracks.forEach(function (track) {
        track.detach();
    });
    if (VideoEl !== '' && typeof VideoEl !== 'undefined') {
        $(VideoEl).remove();
    }
}

function detachParticipantTracks(participant) {
    console.log('Detaching Tracks');
    var tracks = Array.from(participant.tracks.values());
    detachTracks(tracks);
}

var connectOptions = {
    name: roomName
};

if (previewTracks) {
    connectOptions.tracks = previewTracks;
}


function connectcall() {
    Twilio.Video.connect(token, { name: roomName }).then(function (room) {
        console.log('Connected To Room');
        $("#roomname").html("You are currently connected to " + roomName);
        roomGlobal = room;
        console.log(roomGlobal);
        room.participants.forEach(participant => {
            totalParticipants++;
            consultationConnected();
            bootbox.alert({
                title: "Connection Established",
                message: "You are about to begin the session",
                callback: function () {

                    participant.tracks.forEach(track => {
                        document.getElementById('remote-media').appendChild(track.attach());
                    });

                    participant.on('trackAdded', track => {
                        document.getElementById('remote-media').appendChild(track.attach());
                    });


                    var chtml = `<a href="#" class="list-group-item">
                                            <div class="media-left pos-rel">
                                            </div>
                                            <div class="media-body">
                                                <p class="mar-no text-main">`+ participant.identity + `</p>
                                                <small class="text-muteds">Availabe</small>
                                            </div>
                                        </a>`;
                    $("#consultants").html(chtml);
                }
            });

            participant.tracks.forEach(track => {
                document.getElementById('remote-media').appendChild(track.attach());
            });

            participant.on('trackRemoved', track => {
                track.detach().forEach(element => element.remove());
            });


        });

        // this code will be used to connect participant to the room
        // publish / unpublish tracks , disconnect will also be handled here
        room.on('participantConnected', function (participant) {
            totalParticipants++;
            consultationConnected();
            bootbox.alert({
                title: "Connection Established",
                message: "Connection Established. You are about to begin the session",
                callback: function () {
                    var chtml = `<a href="#" class="list-group-item">
                                            <div class="media-left pos-rel">                                        
                                            </div>
                                            <div class="media-body">
                                                <p class="mar-no text-main">`+ participant.identity + `</p>
                                                <small class="text-muteds">Availabe</small>
                                            </div>
                                        </a>`;
                    $("#consultants").html(chtml);
                }

            });

            participant.on('trackAdded', track => {
                document.getElementById('remote-media').appendChild(track.attach());
            });

            participant.tracks.forEach(track => {
                document.getElementById('remote-media').appendChild(track.attach());
            });



            participant.on('trackRemoved', track => {
                track.detach().forEach(element => element.remove());
            });

            participant.on('trackUnsubscribed', track => {
                track.detach().forEach(element => element.remove());
            });

        });

        room.on('disconnected', room => {
            room.localParticipant.tracks.forEach(track => {
                var attachedElements = track.detach();
                attachedElements.forEach(element => element.remove());
            });
            consultationEnded();
        });
        room.once('participantDisconnected', participant => {
            room.disconnect();
            bootbox.alert(participant.identity + " have just finished the session.");
            $("#consultants").html("");
        });

    },

        function (error) {
            console.error('Unable to connect to Room: ' + error.message);
        });
}


var localTracksPromise = previewTracks
    ? Promise.resolve(previewTracks)
    : Twilio.Video.createLocalTracks();

localTracksPromise.then(function (tracks) {
    window.previewTracks = previewTracks = tracks;
    var previewContainer = document.getElementById('local-media');

    if (!previewContainer.querySelector('video')) {
        attachTracks(tracks, previewContainer);

    }
}, function (error) {
});



function leaveRoomIfJoined() {
    if (roomGlobal) {
        roomGlobal.disconnect();
        if (roomGlobal.participants.size > 0) {
            bootbox.alert({
                title: "Session Ended",
                message: "Thank you for your time. Your session is ended and the patient have left the room.",
            })
        }
    }
}



function webSocketsSupport() {
    var supportsWebSockets = 'WebSocket' in window || 'MozWebSocket' in window;
    if (supportsWebSockets) {
        return true;
    }
    else {
        return false;
    }
}


function DeleteFile(Fileid) {
    fileData.delete(Fileid);
    $("#" + Fileid).remove();
    if ($("#filesList tr").length === 0) {
        $("#uploadFiles").attr("disabled", "disabled");
        $("#attachedFiles").val('');
    }
}

function filesUploaded(imgPaths) {
    console.log('Files Uploaded');

    var chat = $.connection.teleSession;
    $.connection.hub.start().done(function () {
        chat.server.filesUploaded(attrs.getAttribute('data-did') + "" + attrs.getAttribute('data-lid'), imgPaths).done(function () {
            console.log('Files Uploaded Successfully');

        });

    });

}


function getCameraDevices(mediaDevices, currentMediaStream) {
    var html = '';
    mediaDevices.forEach(mediaDevice => {
        if (mediaDevice.kind === 'videoinput') {
            html += `<li><input type="radio" name="camera" value="` + mediaDevice.deviceId + `" onclick='updateCameraDevice(this)'/> <label style="font-size:18px;margin-left:10px;">` + mediaDevice.label + `</label></li>`;
            $('#camera-devices').append(html);
        }
    });

    for (var i of currentMediaStream) {
        if (i.kind === 'video') {
            $(".dropdown-menu li input[name=camera][value=" + i.getSettings().deviceId + "]").attr("checked", "checked");
        }
    }
}


function updateCameraDevice(obj) {
    console.log(obj);
    const select = obj.value;
    const localParticipant = roomGlobal.localParticipant;
    if (select !== '') {
        Twilio.Video.createLocalVideoTrack({
            deviceId: { exact: select }
        }).then(function (localVideoTrack) {
            const tracks = Array.from(localParticipant.videoTracks.values());
            localParticipant.unpublishTracks(tracks);
            console.log(localParticipant.identity + " removed track: " + tracks[0].kind);
            detachTracks(tracks, "#local-media video");

            localParticipant.publishTrack(localVideoTrack);
            console.log(localParticipant.identity + " added track: " + localVideoTrack.kind);
            const previewContainer = document.getElementById('local-media');
            attachTracks([localVideoTrack], previewContainer);
        });
    }
}



function consultationConnected() {
    var chat = $.connection.teleSession;
    $.connection.hub.start().done(function () {
        chat.server.consultationStarted(locationId, roomName).done(function () {
            console.log('Consultation Started');
        });
    });
}

function consultationEnded() {

    if (totalParticipants > 0) {
        $("#remote-media video").remove();
        $("#remote-media audio").remove();
        $("#myaudio").find('i').removeClass('fa-microphone-slash');
        $("#myaudio").find('i').addClass('fa-microphone');

        $("#camera").find('i').removeClass('fa-video-slash');
        $("#camera").find('i').addClass('fa-video');

        //just to show that end is ended by showing phone-slash icon
        //after .5s phone-slash icon will be removed
        setTimeout(function () {
            $("#endcall").find('i').removeClass('fa-phone-slash');
            $("#endcall").find('i').addClass('fa-phone');
        }, 500);

        getToken();
        var chat = $.connection.teleSession;
        var participantId = $("#participantId").val();
        if (!participantId) {
            participantId = '';
        }

        $.connection.hub.start().done(function () {
            chat.server.teleSessionConsultationEnded(locationId, participantId, roomName).done(function () {
                console.log('Consultation Ended');
            });
        });
    }
}


function getToken() {
    console.log('getting token');

    const url = '/Common/GetTwilioTokenTeleClinics';
    const data = {
        'Username': roomName
    };
    $.get(url, data, function (resp) {
        if (resp) {
            token = resp;
            connectcall();
        }
    });
}