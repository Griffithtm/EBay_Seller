(() => {
    "use strict";

    const header = document.querySelector("[data-site-header]");
    const syncHeader = () => header?.classList.toggle("is-scrolled", window.scrollY > 12);
    syncHeader();
    window.addEventListener("scroll", syncHeader, { passive: true });

    const dismissToast = (toast) => {
        if (!toast || toast.classList.contains("is-leaving")) return;
        toast.classList.add("is-leaving");
        window.setTimeout(() => toast.remove(), 320);
    };

    document.querySelectorAll("[data-dismiss-toast]").forEach((button) => {
        button.addEventListener("click", () => dismissToast(button.closest(".app-toast")));
    });

    document.querySelectorAll("[data-auto-dismiss]").forEach((toast) => {
        window.setTimeout(() => dismissToast(toast), 5200);
    });

    document.querySelectorAll("[data-password-toggle]").forEach((button) => {
        button.addEventListener("click", () => {
            const input = document.getElementById(button.dataset.target);
            if (!input) return;

            const showing = input.type === "text";
            input.type = showing ? "password" : "text";
            button.textContent = showing ? "Hiện" : "Ẩn";
            button.setAttribute("aria-pressed", String(!showing));
        });
    });

    const passwordInput = document.querySelector("[data-password-strength]");
    const passwordMeter = document.querySelector("[data-password-meter]");
    if (passwordInput && passwordMeter) {
        const meterLabel = passwordMeter.querySelector("small");
        const updateStrength = () => {
            const value = passwordInput.value;
            let score = 0;
            if (value.length >= 8) score += 1;
            if (/[a-z]/.test(value) && /[A-Z]/.test(value)) score += 1;
            if (/\d/.test(value)) score += 1;
            if (/[^A-Za-z0-9]/.test(value) || value.length >= 12) score += 1;

            const states = [
                ["0%", "#d8dde8", "Dùng chữ hoa, chữ thường và số để tăng bảo mật."],
                ["25%", "#d64747", "Mật khẩu còn yếu — hãy thêm chữ hoa và số."],
                ["50%", "#d68316", "Mức trung bình — thêm ký tự đặc biệt sẽ an toàn hơn."],
                ["75%", "#3155d9", "Mật khẩu khá tốt."],
                ["100%", "#119b70", "Mật khẩu mạnh và khó đoán."]
            ];
            const state = value.length === 0 ? states[0] : states[score];
            passwordMeter.style.setProperty("--strength", state[0]);
            passwordMeter.style.setProperty("--meter-color", state[1]);
            if (meterLabel) meterLabel.textContent = state[2];
        };
        passwordInput.addEventListener("input", updateStrength);
        updateStrength();
    }

    document.querySelectorAll("form[data-loading-form], form[data-confirm]").forEach((form) => {
        const resetSubmitButton = (form) => {
            delete form.dataset.submitting;
            const submit = form.querySelector("button[type='submit']");
            if (submit) {
                submit.classList.remove("is-loading");
                submit.removeAttribute("aria-busy");
                const label = submit.querySelector("span");
                if (label && submit.dataset.originalText) {
                    label.textContent = submit.dataset.originalText;
                }
            }
        };

        if (typeof window.jQuery !== "undefined") {
            window.jQuery(form).on("invalid-form.validate", () => resetSubmitButton(form));
        }

        form.addEventListener("submit", function (event) {
            if (!form.checkValidity()) {
                resetSubmitButton(form);
                return;
            }

            if (typeof window.jQuery !== "undefined" && typeof window.jQuery(form).valid === "function") {
                if (!window.jQuery(form).valid()) {
                    resetSubmitButton(form);
                    return;
                }
            }

            if (form.dataset.confirmed === "true") {
                return;
            }

            const confirmation = form.dataset.confirm;
            if (confirmation) {
                event.preventDefault();

                let icon = 'warning';
                let confirmBtnColor = '#dc3545';
                let confirmBtnText = 'Xác nhận thực hiện';
                const action = (form.getAttribute('action') || '').toLowerCase();
                const btnText = (form.querySelector("button[type='submit']")?.textContent || '').toLowerCase();

                if (action.includes('approve') || btnText.includes('phê duyệt')) {
                    icon = 'question';
                    confirmBtnColor = '#198754';
                    confirmBtnText = '✔ Đồng ý phê duyệt';
                } else if (action.includes('delete') || action.includes('cancel') || btnText.includes('xóa') || btnText.includes('hủy')) {
                    icon = 'warning';
                    confirmBtnColor = '#dc3545';
                    confirmBtnText = '❌ Xác nhận Hủy / Xóa';
                }

                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        title: 'Xác nhận thao tác',
                        text: confirmation,
                        icon: icon,
                        showCancelButton: true,
                        confirmButtonColor: confirmBtnColor,
                        cancelButtonColor: '#6c757d',
                        confirmButtonText: confirmBtnText,
                        cancelButtonText: 'Quay lại',
                        reverseButtons: true,
                        focusCancel: true,
                        customClass: {
                            popup: 'shadow-lg border-0 rounded-4',
                            title: 'fw-bold fs-5',
                            confirmButton: 'px-4 py-2 rounded-3 fw-bold shadow-sm',
                            cancelButton: 'px-4 py-2 rounded-3'
                        }
                    }).then((result) => {
                        if (result.isConfirmed) {
                            form.dataset.confirmed = "true";
                            const submit = form.querySelector("button[type='submit']");
                            if (submit) {
                                submit.classList.add("is-loading");
                                submit.setAttribute("aria-busy", "true");
                            }
                            form.submit();
                        }
                    });
                } else {
                    if (window.confirm(confirmation)) {
                        form.dataset.confirmed = "true";
                        form.submit();
                    }
                }
                return;
            }

            if (form.dataset.submitting === "true") {
                event.preventDefault();
                return;
            }

            const submit = form.querySelector("button[type='submit']");
            if (!submit) return;

            form.dataset.submitting = "true";
            submit.classList.add("is-loading");
            submit.setAttribute("aria-busy", "true");
            const label = submit.querySelector("span");
            if (label) {
                if (!submit.dataset.originalText) {
                    submit.dataset.originalText = label.textContent;
                }
                if (submit.dataset.loadingText) {
                    label.textContent = submit.dataset.loadingText;
                }
            }

            // Fallback safety timeout: reset loading after 6s in case form doesn't navigate away
            window.setTimeout(() => resetSubmitButton(form), 6000);
        });
    });

    document.querySelectorAll("[data-close-details]").forEach((button) => {
        button.addEventListener("click", () => button.closest("details")?.removeAttribute("open"));
    });

    const storeName = document.querySelector("[data-store-name-input]");
    const storeDescription = document.querySelector("[data-store-description-input]");
    const storeNamePreview = document.querySelector("[data-store-name-preview]");
    const storeDescriptionPreview = document.querySelector("[data-store-description-preview]");
    const storeInitial = document.querySelector("[data-store-initial]");
    const descriptionFallback = "Mô tả cửa hàng sẽ xuất hiện ở đây. Hãy kể câu chuyện thương hiệu thật ngắn gọn và cuốn hút.";

    const syncStorePreview = () => {
        if (storeName && storeNamePreview) {
            const value = storeName.value.trim();
            storeNamePreview.textContent = value || "Tên cửa hàng";
            if (storeInitial) storeInitial.textContent = value ? value.charAt(0).toLocaleUpperCase("vi-VN") : "S";
        }
        if (storeDescription && storeDescriptionPreview) {
            storeDescriptionPreview.textContent = storeDescription.value.trim() || descriptionFallback;
        }
    };

    storeName?.addEventListener("input", syncStorePreview);
    storeDescription?.addEventListener("input", syncStorePreview);
    syncStorePreview();

    document.querySelectorAll("[data-character-count]").forEach((counter) => {
        const input = document.getElementById(counter.dataset.characterCount);
        if (!input) return;
        const updateCount = () => { counter.textContent = String(input.value.length); };
        input.addEventListener("input", updateCount);
        updateCount();
    });

    const bannerInput = document.querySelector("[data-banner-input]");
    const bannerPreview = document.querySelector("[data-banner-preview]");
    const uploadZone = bannerInput?.closest(".upload-zone");
    const uploadFeedback = document.querySelector("[data-upload-feedback]");
    let temporaryBannerUrl;

    const showUploadMessage = (message, isError = false) => {
        if (!uploadFeedback) return;
        uploadFeedback.textContent = message;
        uploadFeedback.classList.toggle("is-error", isError);
    };

    const updateBannerPreview = () => {
        const file = bannerInput?.files?.[0];
        if (!file || !bannerPreview) return;

        const supported = ["image/jpeg", "image/png", "image/webp"];
        if (!supported.includes(file.type)) {
            bannerInput.value = "";
            showUploadMessage("Tệp chưa hợp lệ. Vui lòng chọn ảnh JPG, PNG hoặc WEBP.", true);
            return;
        }
        if (file.size > 5 * 1024 * 1024) {
            bannerInput.value = "";
            showUploadMessage("Ảnh vượt quá 5 MB. Vui lòng tối ưu ảnh và thử lại.", true);
            return;
        }

        if (temporaryBannerUrl) URL.revokeObjectURL(temporaryBannerUrl);
        temporaryBannerUrl = URL.createObjectURL(file);
        let image = bannerPreview.querySelector("img");
        if (!image) {
            image = document.createElement("img");
            image.alt = "Xem trước ảnh bìa mới";
            bannerPreview.prepend(image);
        }
        image.src = temporaryBannerUrl;
        bannerPreview.querySelector("[data-banner-placeholder]")?.remove();
        bannerPreview.classList.remove("is-empty");
        showUploadMessage(`${file.name} · ${(file.size / 1024 / 1024).toFixed(2)} MB — sẵn sàng tải lên.`);
    };

    bannerInput?.addEventListener("change", updateBannerPreview);
    if (uploadZone) {
        ["dragenter", "dragover"].forEach((eventName) => uploadZone.addEventListener(eventName, () => uploadZone.classList.add("is-dragging")));
        ["dragleave", "drop"].forEach((eventName) => uploadZone.addEventListener(eventName, () => uploadZone.classList.remove("is-dragging")));
    }

    document.querySelectorAll("[data-demo-follow]").forEach((button) => {
        button.addEventListener("click", () => {
            const followed = button.classList.toggle("is-followed");
            button.innerHTML = followed ? "<span>✓</span> Đang theo dõi" : "<span>+</span> Theo dõi cửa hàng";
            button.classList.toggle("btn-primary", !followed);
            button.classList.toggle("btn-success-soft", followed);
        });
    });

    document.querySelectorAll("#mainNavigation a").forEach((link) => {
        link.addEventListener("click", () => {
            const collapseElement = document.getElementById("mainNavigation");
            if (window.innerWidth >= 992 || !collapseElement?.classList.contains("show")) return;
            const collapse = window.bootstrap?.Collapse.getOrCreateInstance(collapseElement);
            collapse?.hide();
        });
    });

    const formatCountdown = (remaining) => {
        if (remaining <= 0) return "Đã kết thúc";
        const totalSeconds = Math.floor(remaining / 1000);
        const days = Math.floor(totalSeconds / 86400);
        const hours = Math.floor((totalSeconds % 86400) / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;
        const clock = [hours, minutes, seconds].map((part) => String(part).padStart(2, "0")).join(":");
        return days > 0 ? `${days} ngày · ${clock}` : clock;
    };

    const countdowns = [...document.querySelectorAll("[data-auction-countdown]")];
    const syncCountdowns = () => {
        countdowns.forEach((countdown) => {
            const end = Date.parse(countdown.dataset.end || "");
            if (Number.isNaN(end)) return;
            const remaining = end - Date.now();
            countdown.textContent = formatCountdown(remaining);
            if (remaining <= 0) countdown.closest(".auction-clock")?.classList.add("is-ended");
        });
    };
    if (countdowns.length > 0) {
        syncCountdowns();
        window.setInterval(syncCountdowns, 1000);
    }

    document.querySelectorAll("[data-number-step]").forEach((button) => {
        button.addEventListener("click", () => {
            const input = document.getElementById(button.dataset.target);
            if (!input) return;
            const step = Number(button.dataset.numberStep || 0);
            const minimum = Number(input.min || 0);
            input.value = String(Math.max(minimum, Number(input.value || 0) + step));
            input.dispatchEvent(new Event("input", { bubbles: true }));
        });
    });

    document.querySelectorAll("[data-product-gallery]").forEach((gallery) => {
        const mainImage = gallery.querySelector("[data-gallery-main]");
        gallery.querySelectorAll("[data-gallery-thumb]").forEach((thumb) => {
            thumb.addEventListener("click", () => {
                if (!mainImage || !thumb.dataset.src) return;
                mainImage.src = thumb.dataset.src;
                gallery.querySelectorAll("[data-gallery-thumb]").forEach((item) => item.classList.remove("is-active"));
                thumb.classList.add("is-active");
            });
        });
    });

    const productForm = document.querySelector("[data-product-form]");
    if (productForm) {
        const titleInput = productForm.querySelector("[data-product-title]");
        const descriptionInput = productForm.querySelector("[data-product-description]");
        const categoryInput = productForm.querySelector("[data-product-category]");
        const priceInput = productForm.querySelector("[data-product-price]");
        const modeInputs = [...productForm.querySelectorAll("[data-listing-mode]")];
        const auctionSettings = productForm.querySelector("[data-auction-settings]");
        const titlePreview = productForm.querySelector("[data-product-title-preview]");
        const descriptionPreview = productForm.querySelector("[data-product-description-preview]");
        const categoryPreview = productForm.querySelector("[data-product-category-preview]");
        const pricePreview = productForm.querySelector("[data-product-price-preview]");
        const typePreview = productForm.querySelector("[data-listing-preview-type]");
        const initialPreview = productForm.querySelector("[data-product-initial]");
        const imageInput = productForm.querySelector("[data-product-image-input]");
        const imagePreview = productForm.querySelector("[data-product-image-preview]");
        const previewMedia = productForm.querySelector("[data-listing-preview-media]");
        const dropzone = productForm.querySelector("[data-product-dropzone]");
        const uploadStatus = productForm.querySelector("[data-product-upload-status]");
        const progressLabel = document.querySelector("[data-listing-progress]");
        const progressRing = progressLabel?.closest(".listing-progress");
        let productImageUrls = [];

        const selectedMode = () => modeInputs.find((input) => input.checked)?.value === "true";
        const hasProductImage = () => Boolean(imageInput?.files?.length || imagePreview?.querySelector("img"));
        const syncListingPreview = () => {
            const title = titleInput?.value.trim() || "Tiêu đề sản phẩm của bạn";
            const description = descriptionInput?.value.trim() || "Mô tả ngắn về sản phẩm sẽ xuất hiện tại đây.";
            const price = Number(priceInput?.value || 0);
            if (titlePreview) titlePreview.textContent = title;
            if (descriptionPreview) descriptionPreview.textContent = description;
            if (initialPreview) initialPreview.textContent = titleInput?.value.trim().charAt(0).toLocaleUpperCase("vi-VN") || "P";
            if (categoryPreview) categoryPreview.textContent = categoryInput?.selectedOptions?.[0]?.textContent || "Danh mục sản phẩm";
            if (pricePreview) pricePreview.textContent = `${new Intl.NumberFormat("vi-VN").format(Math.max(0, price))} ₫`;
            if (typePreview) typePreview.textContent = selectedMode() ? "Đấu giá" : "Giá cố định";
            auctionSettings?.classList.toggle("is-hidden", !selectedMode());

            const readiness = {
                title: (titleInput?.value.trim().length || 0) >= 8,
                description: (descriptionInput?.value.trim().length || 0) >= 30,
                price: price > 0,
                image: hasProductImage()
            };
            Object.entries(readiness).forEach(([name, ready]) => {
                productForm.querySelector(`[data-ready='${name}']`)?.classList.toggle("is-ready", ready);
            });
            const percent = Object.values(readiness).filter(Boolean).length * 25;
            if (progressLabel) progressLabel.textContent = `${percent}% sẵn sàng`;
            progressRing?.style.setProperty("--listing-progress", `${percent}%`);
        };

        const clearProductImageUrls = () => {
            productImageUrls.forEach((url) => URL.revokeObjectURL(url));
            productImageUrls = [];
        };
        const showProductImageError = (message) => {
            if (!uploadStatus) return;
            uploadStatus.textContent = message;
            uploadStatus.classList.add("is-error");
        };
        const syncProductImages = () => {
            const files = [...(imageInput?.files || [])];
            if (files.length === 0) {
                syncListingPreview();
                return;
            }
            if (files.length > 8) {
                imageInput.value = "";
                showProductImageError("Bạn chỉ có thể tải tối đa 8 ảnh cho một listing.");
                syncListingPreview();
                return;
            }
            const supported = ["image/jpeg", "image/png", "image/webp"];
            const invalid = files.find((file) => !supported.includes(file.type) || file.size > 5 * 1024 * 1024);
            if (invalid) {
                imageInput.value = "";
                showProductImageError(`${invalid.name} không hợp lệ. Chỉ dùng JPG, PNG, WEBP và tối đa 5 MB/ảnh.`);
                syncListingPreview();
                return;
            }

            clearProductImageUrls();
            if (imagePreview) imagePreview.innerHTML = "";
            files.forEach((file) => {
                const url = URL.createObjectURL(file);
                productImageUrls.push(url);
                const figure = document.createElement("figure");
                const image = document.createElement("img");
                const caption = document.createElement("figcaption");
                image.src = url;
                image.alt = `Xem trước ${file.name}`;
                caption.textContent = file.name;
                figure.append(image, caption);
                imagePreview?.append(figure);
            });
            if (previewMedia && productImageUrls[0]) {
                previewMedia.querySelector(".product-placeholder")?.remove();
                let image = previewMedia.querySelector(":scope > img");
                if (!image) {
                    image = document.createElement("img");
                    image.alt = "Ảnh đại diện listing";
                    previewMedia.prepend(image);
                }
                image.src = productImageUrls[0];
            }
            if (uploadStatus) {
                uploadStatus.textContent = `${files.length} ảnh · ${(files.reduce((total, file) => total + file.size, 0) / 1024 / 1024).toFixed(2)} MB — sẵn sàng tải lên.`;
                uploadStatus.classList.remove("is-error");
            }
            syncListingPreview();
        };

        [titleInput, descriptionInput, priceInput].forEach((input) => input?.addEventListener("input", syncListingPreview));
        categoryInput?.addEventListener("change", syncListingPreview);
        modeInputs.forEach((input) => input.addEventListener("change", syncListingPreview));
        imageInput?.addEventListener("change", syncProductImages);
        if (dropzone) {
            ["dragenter", "dragover"].forEach((eventName) => dropzone.addEventListener(eventName, (event) => {
                event.preventDefault();
                dropzone.classList.add("is-dragging");
            }));
            ["dragleave", "drop"].forEach((eventName) => dropzone.addEventListener(eventName, () => dropzone.classList.remove("is-dragging")));
        }
        window.addEventListener("pagehide", clearProductImageUrls);
        syncListingPreview();
    }
})();
