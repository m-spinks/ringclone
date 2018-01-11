$(document).ready(function () {
	$('.modal.help').on('shown.bs.modal', function () {
		$.ajax({
			url: "/api/ringcentral/accountinfo",
			success: function (obj) {
				if (obj && obj.Email) {
					$('#helpContactEmail').val(obj.Email);
				}
			}
		});
	})
    $('#helpSubmitButton').on('click', function () {
        if (!$('#helpMessage').val() || $('#helpMessage').val().length === 0) {
            alert('Please enter a message');
        } else {
            $('.modal.help .modal-dialog').mask("Loading...");
            $.ajax({
                type: "POST",
                url: "/api/help/sendmessage",
                contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
                data: jQuery.param({ message: $('#helpMessage').val(), contactEmail: $('#helpContactEmail').val() }),
                complete: function () {
                    $('.modal.help .modal-dialog').unmask();
                },
                success: function (obj) {
                    if (obj) {
                        alert('We have received your message. You will be hearing from us shortly.');
                        $('#helpMessage').val("");
                        $('.modal.help').modal('hide');
                    } else {
                        alert('Sorry, we encountered an error. Please try submitting your message again.');
                    }
                },
                error: function () {
                    alert('Sorry, we encountered an error. Please try submitting your message again.');
                }
            });
        }
    });
});
