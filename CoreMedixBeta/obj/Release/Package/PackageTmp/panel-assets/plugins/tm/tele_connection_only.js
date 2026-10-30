function establishconnection(r,tk) {
    Twilio.Video.connect(tk, { name: r }).then(function (room) {

        //For getting Incoming Call
        room.on('participantConnected', function (participant) {

            show_incoming_call_modal(participant.identity);

        });

    },

        function (error) {
            console.error('Unable to connect to Room: ' + error.message);
        });
}
function show_incoming_call_modal(log_id) {


    //Getting more details
    var url = '/Telemedicine/IncomingRequest';
    var data = { 'case_id': log_id };
    $.post(url, data, function (response, status) {
        if (response) {
            $("#tele_pat_name").html(response.patient.Name);
            var chtml = ``;
            $.each(response.complaints, function (index, value) {

                chtml += `<li>` + value.CheifComplaints + '</li>';
            });
            $("#tele_complaints").html(chtml);
            $("#tele_operator").html(response.Nurse.Username);
            redirectUrl = '/Telemedicine/VideoCheckup?patient_username=' + response.log.patient_id + '&pid=' + response.log.healthrecord_id;

            $("#in_call_modal").modal({
                backdrop: 'static',
                show: true,
            });

        }
    });


}

