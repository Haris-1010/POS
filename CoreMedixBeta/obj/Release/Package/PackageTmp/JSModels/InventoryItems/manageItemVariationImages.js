(function ($) {
    const _$variationsTable = $('#variationsTable');

    const _$variationImagesModal = $('#variationImagesModal'),
        _$variationImagesForm = _$variationImagesModal.find('form');

    const _$imagePreviewContainer = _$variationImagesModal.find('.js-image-preview-container');

    let selectedImages = [];

    $(function () {
        _$variationImagesForm.validate();

        _$variationImagesModal.on('hidden.bs.modal', function () {
            $('.modal-backdrop.fade.show').remove();
            _$variationImagesForm.trigger('reset');
            _$variationImagesForm.find('input[name="VariationID"]').val('0');
            _$imagePreviewContainer.html('');
            selectedImages = [];
        });

        _$variationImagesModal.on('click', '.js-save-btn', async function () {
            const _$saveBtn = $(this);
            _$saveBtn.text('Saving...');
            $('button').prop('disabled', true);

            const url = SaveVariationImages;
            const data = {
                Images: selectedImages
            };

            try {
                const resp = await $.post(url, data);
                const { Status: status, Message: message } = resp;
                if (status) {
                    toastr.success(message, "", { timeOut: 500 });
                    _$variationImagesModal.modal('hide');
                    /*                    getVariations();*/
                } else {
                    toastr.error(message, "", { timeOut: 500 });
                }
            } catch (error) {
                toastr.error("Failed to save Variation due to a technical error.", "", { timeOut: 500 });
            } finally {
                _$saveBtn.text('Save').prop('disabled', false);
                $('button').prop('disabled', false);
            }
        });
    });

    _$variationsTable.on('click', '.js-image', async function () {
        let Id = $(this).data('id');

        const url = GetVariationImagesUrl;
        const data = {
            ID: Id
        };
        try {
            const resp = await $.post(url, data);
            const { Status: status, Message: message, Data: response } = resp;
            if (status) {
                $.each(response, function (index, img) {
                    previewImagesUsingURLs(img.ImageURL, img.ImageName, img.VariationID);
                });
                _$variationImagesForm.find('input[name="VariationID"]').val(Id);
                _$variationImagesModal.modal("show");
            } else {
                _$variationImagesForm.find('input[name="VariationID"]').val(Id);
                _$variationImagesModal.modal("show");
            }
        } catch (error) {
            toastr.error("An error occurred", "", { timeOut: 500 });
        }
    });

    $(document).off("change", ".js-image-input").on("change", ".js-image-input", function () {
        const allowedExtensions = ["jpg", "jpeg", "png", "webp"];
        const input = $(this);
        const files = input.prop("files");
        let isValid = true;

        $.each(files, function (index, file) {
            const fileExtension = file.name.split('.').pop().toLowerCase();
            if (!allowedExtensions.includes(fileExtension)) {
                isValid = false;
                toastr.error(`Invalid file type: ${file.name}. Only ${allowedExtensions.join(", ")} are allowed.`, "", { timeOut: 1000 });
                input.val("");
                return false;
            }
        });

        if (isValid) {
            let variationId = _$variationImagesForm.find('input[name="VariationID"]').val();
            previewImages(input, variationId);
        }
    });

    function previewImagesUsingURLs(imageUrl, imageName, variationID) {
        const imageObject = {
            VariationID: variationID,
            ImageURL: imageUrl,
            ImageName: imageName,
        };
        selectedImages.push(imageObject);

        const imgWrapper = $("<div>")
            .css({
                display: "inline-block",
                position: "relative",
                marginRight: "5px",
            });
        const isBase64 = /^[A-Za-z0-9+/=]+$/.test(imageUrl);

        const imgSrc = isBase64
            ? `data:image/png;base64,${imageUrl}` // prefix for base64
            : imageUrl; // direct URL
        const img = $("<img>")
            .attr("src", imageUrl)
            .css({
                width: "5rem",
                borderTopRightRadius: "10px"
            });

        const deleteBtn = $("<button>")
            .text("x")
            .css({
                position: "absolute",
                top: "0",
                right: "0",
                background: "red",
                color: "white",
                border: "none",
                borderRadius: "50%",
                cursor: "pointer",
                width: "20px",
                height: "20px",
                lineHeight: "16px",
                textAlign: "center",
                padding: "0",
            })
            .on("click", function () {
                selectedImages = selectedImages.filter(
                    (imgObj) => imgObj.ImageURL !== imageObject.ImageURL
                );

                imgWrapper.remove();
            });

        imgWrapper.append(img).append(deleteBtn);
        _$imagePreviewContainer.append(imgWrapper);
    }

    function previewImages(input, variationID) {
        const files = input.prop("files");

        $.each(files, function (index, file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                const base64Data = e.target.result.split(',')[1];

                const imageObject = {
                    VariationID: variationID,
                    ImageURL: base64Data,
                    ImageName: file.name,
                };
                selectedImages.push(imageObject);

                const imgWrapper = $("<div>")
                    .css({
                        display: "inline-block",
                        position: "relative",
                        marginRight: "5px",
                    });

                const img = $("<img>")
                    .attr("src", e.target.result)

                    .css({
                        width: "5rem",
                        borderTopRightRadius: "10px"
                    });


                const deleteBtn = $("<button>")
                    .text("x")
                    .css({
                        position: "absolute",
                        top: "0",
                        right: "0",
                        background: "red",
                        color: "white",
                        border: "none",
                        borderRadius: "50%",
                        cursor: "pointer",
                        width: "20px",
                        height: "20px",
                        lineHeight: "16px",
                        textAlign: "center",
                        padding: "0",
                    })
                    .on("click", function () {
                        selectedImages = selectedImages.filter(
                            (imgObj) => imgObj.ImageURL !== imageObject.ImageURL
                        );

                        imgWrapper.remove();
                    });

                imgWrapper.append(img).append(deleteBtn);
                _$imagePreviewContainer.append(imgWrapper);
            };

            reader.readAsDataURL(file);
        });
    }
})(jQuery);