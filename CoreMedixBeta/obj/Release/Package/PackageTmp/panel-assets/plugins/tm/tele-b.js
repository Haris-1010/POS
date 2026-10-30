'use strict';

var Video = require(['https://media.twiliocdn.com/sdk/js/video/v1/twilio-video.min.js']);

var activeRoom;
var previewTracks;
var identity;
var roomName;
var token;
var attrs = document.querySelector('script[data-token][data-room]');
token = attrs.getAttribute('data-token');
roomName = attrs.getAttribute('data-room');

$("#CallMakingModal").modal("show");

// Attach the Tracks to the DOM.
function attachTracks(tracks, container) {
    tracks.forEach(function (track) {
        container.appendChild(track.attach());
    });
}

// Attach the Participant's Tracks to the DOM.
function attachParticipantTracks(participant, container) {
    var tracks = Array.from(participant.tracks.values());
    attachTracks(tracks, container);
}

// Detach the Tracks from the DOM.
function detachTracks(tracks) {
    tracks.forEach(function (track) {
        track.detach().forEach(function (detachedElement) {
            detachedElement.remove();
        });
    });
}

// Detach the Participant's Tracks from the DOM.
function detachParticipantTracks(participant) {
    var tracks = Array.from(participant.tracks.values());
    detachTracks(tracks);
}

// When we are about to transition away from this page, disconnect
// from the room, if joined.
window.addEventListener('beforeunload', leaveRoomIfJoined);


// Bind button to join Room.

console.log("Joining room '" + roomName + "'...");
var connectOptions = {
    name: roomName,
    logLevel: 'debug'
};

if (previewTracks) {
    connectOptions.tracks = previewTracks;
}

Twilio.Video.connect(token, connectOptions).then(roomJoined, function (error) {
    console.log('Could not connect to Twilio: ' + error.message);
});


// Successfully connected!
function roomJoined(room)
{
    bootbox.alert("Welcome to PharmaX. You're now online for any video checkup.");

    window.room = activeRoom = room;
    //console.log("Joined as '" + identity + "'");
    var previewContainer = document.getElementById('local-media');
    if (!previewContainer.querySelector('video')) {
        attachParticipantTracks(room.localParticipant, previewContainer);
    }

    // Log your Client's LocalParticipant in the Room
    const localParticipant = room.localParticipant;
    alert('Connected to the Room as LocalParticipant as ' + localParticipant.identity);



    // Log any Participants already connected to the Room
    room.participants.forEach(participant => {
        alert('Participant ' + participant.identity + ' is connected to the Room');
        participant.tracks.forEach(track => {
            document.getElementById('remote-media').appendChild(track.attach());
            $("#CallMakingModal").modal("show");
        });

        participant.on('trackAdded', track => {
            document.getElementById('remote-media').appendChild(track.attach());
            $("#CallMakingModal").modal("show");
        });
    });

    room.once('participantConnected', participant => {
        alert('Participant ' + participant.identity + ' has connected to the Room');
    });

    room.on('participantDisconnected', function (participant) {
        $("#call_tracks").modal("hide");
    });



    room.on('participantConnected', function (participant) {
        $("#Caller_id").html(participant.identity + " is calling you");
        $("#Incoming_Call").modal("show");

        participant.tracks.forEach(track => {
            document.getElementById('remote-media').appendChild(track.attach());
            $("#CallMakingModal").modal("show");
        });

        participant.on('trackAdded', track => {
            document.getElementById('remote-media').appendChild(track.attach());
            $("#CallMakingModal").modal("show");
        });
    });

}

// Preview LocalParticipant's Tracks.
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
    console.error('Unable to access local media', error);
    console.log('Unable to access Camera and Microphone');
});


$("#InviteAccept").click(function () {


    $("#Incoming_Call").modal("hide");

    $("#call_tracks").modal("show");

});

// Leave Room.
function leaveRoomIfJoined() {
    if (activeRoom) {
        activeRoom.disconnect();
    }
}
