'use strict';
var attrs = document.querySelector('script[data-token][data-room][data-patienttoken][data-patientname][data-doctorname][data-doctorimage][data-patientun][data-doctorun][data-location][data-notify][data-appId]');

var browserSupported = true;
var camerAudioPermissions = true;
const patientSource = 'Web';



var Video = require(['https://media.twiliocdn.com/sdk/js/video/v1/twilio-video.min.js']);
var activeRoom;
var previewTracks;
var roomName;
var token;
var roomGlobal;
//LocalParticipant 
var localparticipant;

token = attrs.getAttribute('data-token');
roomName = attrs.getAttribute('data-room');


$(document).ready(function () {

    var teleSession = $.connection.teleSession;

    teleSession.client.shareFiles = function (imgPaths) {
        if ($("#filesList").length !== 0) {
            var html = '';
            $.each(imgPaths, function (i, v) {
                html += `<li>
                    <div class="files-list-container">
                        <div class="file-img">
                            ` + checkIfFileIsImage(v.FilePath)  + `
                        </div>
                        <div class="file-details">
                            <p data-toggle="tooltip" title="${v.FileName}">` + v.FileName +  `</p>
                            <div class="action-buttons">
                                <a class="btn" style="background-color: #294f75;border-color: #25476a !important;color: #fff;cursor:pointer;" onclick=viewFile("` + v.FilePath + `",'` + v.FileName + `')>View</a>
                                <a class="btn btn-success" href="` + v.FilePath + `" target="_blank">Download</a>
                            </div>
                        </div>
                    </div>
                </li>`;
            });
            $("#filesList").append(html);
            $("#uploadingFileAlert").show();
            setTimeout(function () {
                $("#uploadingFileAlert").hide();
            }, 1000);
        }
    };

    $.connection.hub.start().done(function () {
        teleSession.server.addUserLevelGroups(attrs.getAttribute('data-doctorun') + attrs.getAttribute('data-location'), attrs.getAttribute('data-doctorun'), attrs.getAttribute('data-location'), '').done(function () {
            console.log('Connection Established');
        });
    }).fail(function (err) { console.log(err); });


    $('#fm.sidebar').on('click', function () {
        $(this).toggleClass('active');
        $('#fmaside').animate({ width: 'toggle' }, 200);
    });

    $("#files.sidebar").on('click', function () {
        $(this).toggleClass('active');
        $("#filesAside").animate({ width: 'toggle' }, 200);
    });

    $("#cancel_call").on('click', function () {
        $("#CallMakingModal").modal("hide");
        leaveRoomIfJoined();
        var audio = document.getElementById("outgoing");
        audio.pause();
    });

    GetFreqMedList();
   
    $("#AppDate").datepicker('setDate', new Date());

    $('#searchFreqMedicine').on('input', function (e) {
        $('.freqmedicine').fadeOut(100);
        var txt = $('#searchFreqMedicine').val().toUpperCase();
        if (txt) {
            $('.freqmedicine:contains("' + txt + '")').fadeIn(100);
        }
        else {
            $('.freqmedicine').fadeIn(100);
        }
    });
    $("#app_check").change(function () {
        if ($("#app_check").is(":checked")) {
            $("#stimediv").show();
            $("#etimediv").show();
        }
        else {
            $("#stimediv").hide();
            $("#etimediv").hide();
        }
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
            $(this).find('i').removeClass('fa-video-slash');
            $(this).find('i').addClass('fa-video');

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

            roomGlobal.localParticipant.audioTracks.forEach(function (audioTrack){
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

    $("#endcall").click(function () {
        leaveRoomIfJoined();
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
                debugger;
                navigator.mediaDevices.enumerateDevices().then(function (devices) { getCameraDevices(devices, stream.getTracks()) });
                connectcall();
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
    function showcall(status) {
        var audio = document.getElementById("outgoing");
        if (status === "show") {

            audio.play();
            $("#CallMakingModal").modal("show");

        }
        else {
            audio.pause();
            $("#CallMakingModal").modal("hide");

        }
    }

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
        var tracks = Array.from(participant.tracks.values());
        detachTracks(tracks);
    }

    // When we are about to transition away from this page, disconnect
    // from the room, if joined.
    window.addEventListener('beforeunload', leaveRoomIfJoined);



    // Bind button to join Room.


    var connectOptions = {
        name: roomName,
    };

    if (previewTracks) {
        connectOptions.tracks = previewTracks;
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

    },
        function (error) {
            console.error('Unable to access local media', error);
            console.log('Unable to access Camera and Microphone');
        });


    function connectcall() {
        showcall("show");
        Twilio.Video.connect(token, { name: roomName }).then(function (room) {

            //bootbox.alert('Welcome! You are connected and ready to begin.');
            saveTeleSession(true);

            if (patientSource === 'Web' && attrs.getAttribute("data-notify") === "True") {
                PushNotification();
            }


            localparticipant = room.localParticipant;
            roomGlobal = room;
            room.participants.forEach(participant => {
                showcall("hide");
                updateConsultingNow();
                console.log('All Participants Connected');

                participant.on('trackAdded', track => {
                    document.getElementById("remote-media").appendChild(track.attach());
                    $("#remote-media").css("background-color", "black");
                });

                participant.tracks.forEach(track => {
                    document.getElementById("remote-media").appendChild(track.attach());
                    $("#remote-media").css("background-color", "black");
                });

                participant.on('trackRemoved', track => {
                    track.detach().forEach(element => element.remove());
                });
            });

            room.on('participantConnected', function (participant) {
                showcall("hide");
                updateConsultingNow();
                console.log('Participant Connected');
                participant.on('trackAdded', track => {
                    document.getElementById("remote-media").appendChild(track.attach());
                    $("#remote-media").css("background-color", "black");

                });
                participant.tracks.forEach(track => {
                    document.getElementById("remote-media").appendChild(track.attach());
                    $("#remote-media").css("background-color", "black");

                });

               

                participant.on('trackRemoved', track => {
                    track.detach().forEach(element => element.remove());
                });

                participant.on('trackUnsubscribed', track => {
                    track.detach().forEach(element => element.remove());
                });


            });

            room.on('disconnected', function () {
                console.log('Disconnecting');
                saveTeleSession(false);
                consultationEnded();
                if (previewTracks) {
                    previewTracks.forEach(function (track) {
                        track.stop();
                    });
                    previewTracks = null;
                }
                detachParticipantTracks(localparticipant);
                room.participants.forEach(detachParticipantTracks);
                roomGlobal = null;
            });
        },

            function (error) {
                console.error('Unable to connect to Room: ' + error.message);
            });

    }



    function roomJoined(room) {
        alert("Successfully Connected to room");
        window.room = activeRoom = room;

        room.on('participantConnected', function (participant) {

            //alert("Participant '" + participant.identity + "' connected");

            $("#Caller_id").html(participant.identity + " is calling you");
            $("#Incoming_Call").modal("show");


            participant.tracks.forEach(track => {
                document.getElementById('remote-media').appendChild(track.attach());
            });

            participant.on('trackAdded', track => {
                document.getElementById('remote-media').appendChild(track.attach());
            });

            participant.on('trackRemoved', track => {
                document.querySelector('#remote-media video').remove();
                track.detach();
            });
        });

        room.participants.forEach(participant => {
            participant.tracks.forEach(track => {
                document.getElementById('remote-media').appendChild(track.attach());
            });
            $("#Caller_id").html(participant.identity + " is calling you");
            $("#Incoming_Call").modal("show");
        });

    }

    // Successfully connected!




    // Leave Room.
function leaveRoomIfJoined() {  

    if (roomGlobal && roomGlobal !== null) {
        roomGlobal.disconnect();
    }
}


    function PushNotification() {
        var url = '/Prescriptions/PushNotification';
        var patientName = attrs.getAttribute('data-patientname');
        var roomName = attrs.getAttribute('data-room');
        var doctorName = attrs.getAttribute('data-doctorname');
        var patientToken = attrs.getAttribute('data-patienttoken');
        var doctorImage = attrs.getAttribute('data-doctorimage');


        var data = {
            'PatientName': patientName,
            'RoomName': roomName,
            'DoctorName': doctorName,
            'PatientToken': patientToken,
            'DoctorImage': doctorImage
        };

        $.post(url, data, function (data, status) {
            if (data === true) {
                console.log("Notification Sent");
            }
            else {
                console.log("Notification Not Sent");

            }
        });      
    }


function saveTeleSession(status) {

        var url = '/Telemedicine/SaveTeleSession';
        var data = {
            id: $("#teleSessionId").val(),
            start_timestamp: $("#teleSessionStartTime").val(),
            patient_id: attrs.getAttribute('data-patientun'),
            doctor_id: attrs.getAttribute('data-doctorun'),
            operator_id: attrs.getAttribute('data-doctorun'),
            location_id: attrs.getAttribute('data-location'),
            consultationStatus: status
        };
        $.post(url, data, function (response) {
            if (response !== null) {
                $("#teleSessionId").val(response.id);
                $("#teleSessionStartTime").val(response.start_timestamp);
            }
        });
    }



    function SaveFrequentMed(medicine, form, grams, freq, inst) {
        var data = {
            'Medicine': medicine,
            'FormType': form,
            'Grams': grams,
            'Frequency': freq,
            'MedicineNotes': inst
        };
        var url = '/Prescriptions/Frequent_Medicines_Save';
        $.post(url, data, function (response, status) {
            if (response) {
                var lhtml = `           <div class="row freqmedicine .freq_` + data.id + `" id="freq_` + data.id + `">
                            <div class="col-md-1" style='color:black;'>
                                <a href="#/" onclick="FreqMedicineToList('`+ data.Medicine + `','` + data.FormType + `','` + data.Grams + `','` + data.Frequency + `','` + data.MedicineNotes + `')" title="Add to List"><span class="fa fa-plus"></span></a>
                                <a href="#/" onclick="remFreqMed('`+ data.id + `')" title="Remove from Frequent Medicines"><span class="fa fa-times"></span></a>

                            </div>
                            <div class="col-md-11">
                                <small>
                                    <b class='freqMedTitle'>`+ data.Medicine + `</b> - ` + data.Grams + ` <br />
                                    <label>`+ data.FormType + `</label> | <label>` + data.Frequency + `</label>
                                </small>
                            </div>

                        </div> <hr style="margin:5px">`;

                $("#FrequentMedList").append(lhtml);
            }
        });
    }
    function remFreqMed(id) {
        bootbox.confirm({
            title: "Remove medicine from frequent list",
            message: "Are you sure you want to remove this medicine from your frequent medicine list? This action is irreversable.",
            buttons: {
                cancel: {
                    label: '<i class="fa fa-times"></i> Cancel'
                },
                confirm: {
                    label: '<i class="fa fa-check"></i> Yes Remove this from my list'
                }
            },
            callback: function (result) {
                if (result) {
                    var data = { 'id': id };
                    var url = "/Prescriptions/Frequent_Medicines_Remove";
                    $.post(url, data, function (response, status) {
                        if (data) {
                            $(".freq_" + id).remove();
                        }
                    });
                }
            }
        });


    }

    function GetFreqMedList() {

        var url = "/Prescriptions/Frequent_Medicines_Get";
        $.post(url, null, function (data, status) {
            if (data) {
                $.each(data, function (index, item) {
                    var lhtml = ` <div class="row freqmedicine freq_` + item.id + `" id="freq_` + item.id + `">
                            <div class="col-md-1" style='color:black;'>
                                <a href="#/" onclick="FreqMedicineToList('`+ item.Medicine + `','` + item.FormType + `','` + item.Grams + `','` + item.Frequency + `','` + item.MedicineNotes + `')" title="Add to List"><span class="fa fa-plus"></span></a>
                                <a href="#/" onclick="remFreqMed('`+ item.id + `')" title="Remove from Frequent Medicines"><span class="fa fa-times"></span></a>

                            </div>
                            <div class="col-md-11">
                                <small>
                                    <b class='freqMedTitle'>`+ item.Medicine + `</b> - ` + item.Grams + ` <br />
                                    <label>`+ item.FormType + `</label> | <label>` + item.Frequency + `</label>
                                </small>
                            </div>

                        </div><hr style="margin:5px">`;
                    $("#FrequentMedList").append(lhtml);
                });

            }
        });
    }
function FreqMedicineToList(medicine, form, grams, freq, inst) {
    if (form !== "" && typeof form !== "undefined" && form !== null && form !== 'null') {
        medicine += " - " + form ;
    }
    if (grams !== "" && typeof grams !== "undefined" && grams !== null && grams !== 'null') {
        medicine += " - " + grams;

    }
        $("#searchmedicine").val(medicine);
        $("#formtype").val(form);
        $("#grams").val(grams);
        $("#freq").val(freq);
        $("#inst").val(inst);
}



function updateAppStatus() {
    var appId = attrs.getAttribute("data-appId");
    console.log(appId);

    if (appId !== '' && appId !== null && typeof appId !== "undefined" && appId !== "0") {
        var chat = $.connection.teleSession;

        $.connection.hub.start().done(function () {
            chat.server.updateAppoitmentStatus(attrs.getAttribute('data-doctorun') + '' + attrs.getAttribute('data-location'), appId).done(function () {
                console.log('Appointment Status Updated');
            });
        });

    }


}

function updateConsultingNow() {
    var patId = attrs.getAttribute("data-patientun");

    if (patId !== '' && patId !== null && typeof patId !== "undefined" && patId !== "0") {
        var chat = $.connection.teleSession;

        $.connection.hub.start().done(function () {
            chat.server.updateConsultingNow(attrs.getAttribute('data-doctorun') + '' + attrs.getAttribute('data-location'), patId).done(function () {
                console.log('Consulting Now Updated');
            });
        });

    }
}


function consultationEnded() {
    var patId = attrs.getAttribute("data-patientun");
    var loc = attrs.getAttribute("data-location");

    if (patId !== '' && patId !== null && typeof patId !== "undefined" && patId !== "0") {
        var chat = $.connection.teleSession;

        $.connection.hub.start().done(function () {
            chat.server.consultationEnded(attrs.getAttribute('data-doctorun') + '' + attrs.getAttribute('data-location'), patId,loc).done(function () {
                console.log('Consultation Ended');
            });
        });

    }
}



function viewFile(path, name) {

    var val = path.toLowerCase(),
        regex = new RegExp("(.*?)\.(docx|doc|pdf)$");

    if (!(regex.test(val))) {
        var imobj = { 'src': path, 'title': name };
        var photos = [];
        photos.push(imobj);
        var options = {
            draggable: true,
            resizable: true,
            movable: true,
            keyboard: true,
            title: true,
            footToolbar: ['zoomIn', 'zoomOut', 'fullscreen', 'actualSize', 'rotateRight'],
            zIndex: 999999999991090,

        };

        var viewer = new PhotoViewer(photos, options);

    }
    else {
        $("#documentName").text(name);
        var iframePath = "https://docs.google.com/gview?url=" + path + "&embedded=true";
        $("#documentIFrame").attr('src', iframePath);
        $("#docViewerModal").modal('show');
    }
}


function checkIfFileIsImage(filepath) {
    var val = filepath.toLowerCase(),
        regex = new RegExp("(.*?)\.(docx|doc|pdf)$");

    if (!(regex.test(val))) {
        return `<img class="img-responsive" src="` + filepath + `" alt="1">`;
    }
    else {
        return `<i class="fas fa-file" style="color: #294f75 !important"></i>`;
    }
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