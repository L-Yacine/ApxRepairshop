(function () {
    "use strict";

    var PUBLIC = window.PUBLIC || {};
    var STR = PUBLIC.strings || {};
    var URLS = PUBLIC.urls || {};
    var numberLocale = PUBLIC.isRtl ? 'ar-DZ' : (PUBLIC.culture === 'en' ? 'en-US' : 'fr-FR');
    var CURRENCY = STR.currency || "";

    function ready(fn) {
        if (document.readyState !== "loading") { fn(); }
        else { document.addEventListener("DOMContentLoaded", fn); }
    }

    // Dynamic price formatting helper
    function formatDZD(n) {
        return n.toLocaleString(numberLocale, { maximumFractionDigits: 2 }) + " " + CURRENCY;
    }

    // Global Toast Notification System
    function showToast(message, type) {
        var container = document.querySelector(".public-toast-container");
        if (!container) {
            container = document.createElement("div");
            container.className = "public-toast-container";
            document.body.appendChild(container);
        }
        var toast = document.createElement("div");
        toast.className = "public-toast public-toast--" + (type || "success");
        toast.textContent = message;
        container.appendChild(toast);

        // Slide in
        setTimeout(function () {
            toast.classList.add("public-toast--show");
        }, 10);

        // Auto destroy after 3s
        setTimeout(function () {
            toast.classList.remove("public-toast--show");
            var removeToast = function () {
                toast.remove();
                toast.removeEventListener("transitionend", removeToast);
            };
            toast.addEventListener("transitionend", removeToast);
        }, 3000);
    }

    // Animate navbar cart count badges
    function updateCartBadges(count) {
        document.querySelectorAll(".public-cart-badge").forEach(function (badge) {
            badge.textContent = count;
            badge.setAttribute("data-cart-count", count);
            if (count === 0) {
                badge.classList.add("public-cart-badge--zero");
            } else {
                badge.classList.remove("public-cart-badge--zero");
            }

            // Trigger scale pop keyframes
            badge.classList.remove("badge-pop");
            badge.offsetHeight; // Force reflow
            badge.classList.add("badge-pop");
        });
    }

    // AJAX cart quantity sync
    function updateCartItemQty(form, newQty) {
        var cartItem = form.closest(".public-cart-item");
        var quantityInput = form.querySelector('input[name="quantityDisplay"]');
        var originalValue = quantityInput ? quantityInput.value : "1";

        var buttons = form.querySelectorAll("button");
        buttons.forEach(function (btn) { btn.disabled = true; });

        var token = form.querySelector('input[name="__RequestVerificationToken"]').value;
        var inventoryPartId = form.querySelector('input[name="inventoryPartId"]').value;

        var body = new URLSearchParams();
        body.append("__RequestVerificationToken", token);
        body.append("inventoryPartId", inventoryPartId);
        body.append("quantity", newQty);

        fetch(URLS.cartUpdate || "/Cart/UpdateQuantity", {
            method: "POST",
            body: body,
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
                "Accept": "application/json"
            }
        })
        .then(function (res) {
            return res.ok ? res.json() : { success: false };
        })
        .then(function (data) {
            buttons.forEach(function (btn) { btn.disabled = false; });
            if (data.success) {
                updateCartBadges(data.cartCount);

                if (data.removed) {
                    animateRemoveCartItem(cartItem, data.subtotal);
                } else {
                    if (quantityInput) quantityInput.value = newQty;
                    var totalEl = cartItem.querySelector(".public-cart-item__total");
                    if (totalEl) {
                        totalEl.textContent = formatDZD(data.itemTotal);
                    }
                    updateCartTotals(data.subtotal);
                }
            } else {
                if (quantityInput) quantityInput.value = originalValue;
                showToast(STR.cartUpdateFailed || "Failed to update quantity.", "error");
            }
        })
        .catch(function () {
            buttons.forEach(function (btn) { btn.disabled = false; });
            if (quantityInput) quantityInput.value = originalValue;
            showToast(STR.networkError || "Network error.", "error");
        });
    }

    // AJAX cart item removal
    function removeCartItem(form) {
        var cartItem = form.closest(".public-cart-item");
        var removeBtn = form.querySelector(".public-cart-item__remove");
        if (removeBtn) removeBtn.disabled = true;

        var token = form.querySelector('input[name="__RequestVerificationToken"]').value;
        var inventoryPartId = form.querySelector('input[name="inventoryPartId"]').value;

        var body = new URLSearchParams();
        body.append("__RequestVerificationToken", token);
        body.append("inventoryPartId", inventoryPartId);

        fetch(URLS.cartRemove || "/Cart/Remove", {
            method: "POST",
            body: body,
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
                "Accept": "application/json"
            }
        })
        .then(function (res) {
            return res.ok ? res.json() : { success: false };
        })
        .then(function (data) {
            if (data.success) {
                updateCartBadges(data.cartCount);
                animateRemoveCartItem(cartItem, data.subtotal);
            } else {
                if (removeBtn) removeBtn.disabled = false;
                showToast(STR.cartRemoveFailed || "Could not remove the item.", "error");
            }
        })
        .catch(function () {
            if (removeBtn) removeBtn.disabled = false;
            showToast(STR.networkError || "Network error.", "error");
        });
    }

    // Animate removal and fallback empty state
    function animateRemoveCartItem(cartItem, newSubtotal) {
        if (!cartItem) return;
        cartItem.style.transition = "all 0.3s ease";
        cartItem.style.opacity = "0";
        cartItem.style.transform = "translateY(-20px)";

        setTimeout(function () {
            cartItem.remove();
            updateCartTotals(newSubtotal);

            // Swap cart interface if empty
            var remaining = document.querySelectorAll(".public-cart-item");
            if (remaining.length === 0) {
                var cartContainer = document.querySelector(".public-cart");
                if (cartContainer) {
                    var title = STR.cartTitle || "";
                    var itemsLabel = STR.cartItems || "";
                    var emptyTitle = STR.cartEmptyTitle || "";
                    var emptyHint = STR.cartEmptyHint || "";
                    var browseParts = STR.browseParts || "";
                    var browseUrl = URLS.catalogBrands || "/Catalog/Brands";
                    cartContainer.innerHTML =
                        '<div class="public-cart__head">' +
                        '  <h2 class="public-cart__title">' + title + '</h2>' +
                        '  <span class="public-cart__count">0 ' + itemsLabel + '</span>' +
                        '</div>' +
                        '<div class="public-empty">' +
                        '  <p class="public-empty__title">' + emptyTitle + '</p>' +
                        '  <p>' + emptyHint + '</p>' +
                        '  <div class="public-empty__actions">' +
                        '    <a href="' + browseUrl + '" class="public-btn public-btn--primary">' + browseParts + '</a>' +
                        '  </div>' +
                        '</div>';
                }
            }
        }, 300);
    }

    // Update totals and subtotals on cart UI
    function updateCartTotals(subtotal) {
        var subtotalCells = document.querySelectorAll(".public-cart__totals-row span:last-child");
        if (subtotalCells.length > 0) {
            subtotalCells[0].textContent = formatDZD(subtotal);
        }
        var totalValCell = document.querySelector(".public-cart__totals-row--total .value");
        if (totalValCell) {
            totalValCell.textContent = formatDZD(subtotal);
        }

        var countBadge = document.querySelector(".public-cart-badge");
        var count = countBadge ? parseInt(countBadge.textContent, 10) || 0 : 0;
        var headCount = document.querySelector(".public-cart__count");
        if (headCount) {
            headCount.textContent = count + " " + (STR.cartItems || "");
        }
    }

    // AJAX cart addition
    function submitCartAdd(form) {
        var submitBtn = form.querySelector('button[type="submit"]');
        var originalBtnText = submitBtn ? submitBtn.innerHTML : "";
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<span class="select-loading" style="display:inline-block; width:16px; height:16px; vertical-align:middle;"></span> ' + (STR.cartAdding || "") + '';
        }

        var formData = new FormData(form);
        var action = form.getAttribute("action");

        fetch(action, {
            method: "POST",
            body: formData,
            headers: {
                "Accept": "application/json"
            }
        })
        .then(function (res) {
            return res.ok ? res.json() : { success: false, message: STR.unexpectedError || "" };
        })
        .then(function (data) {
            if (data.success) {
                updateCartBadges(data.cartCount);
                showToast(data.message || STR.cartAdded || "", "success");

                if (submitBtn) {
                    submitBtn.innerHTML = '✓ ' + (STR.cartAddedShort || "");
                    var originalBg = submitBtn.style.backgroundColor;
                    submitBtn.style.backgroundColor = 'var(--public-success)';
                    setTimeout(function () {
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = originalBtnText;
                        submitBtn.style.backgroundColor = originalBg;
                    }, 1500);
                }
            } else {
                showToast(data.message || STR.cartAddFailed || "", "error");
                if (submitBtn) {
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = originalBtnText;
                }
            }
        })
        .catch(function () {
            showToast(STR.connectionError || "", "error");
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalBtnText;
            }
        });
    }

    // AJAX repair status lookup — shows the result in the landing-page modal
    function lookupRepairStatus(form) {
        var modalEl = document.getElementById("repairStatusModal");
        var modalBody = modalEl ? modalEl.querySelector("[data-repair-status-body]") : null;
        var lookupUrl = form.getAttribute("data-lookup-url");
        var input = form.querySelector('input[name="jobCode"]');
        var jobCode = input ? input.value.trim() : "";

        if (!modalEl || !modalBody || !window.bootstrap || !lookupUrl || !jobCode) {
            return false;
        }

        var submitBtn = form.querySelector('button[type="submit"]');
        var originalBtnText = submitBtn ? submitBtn.innerHTML : "";
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<span class="select-loading" style="display:inline-block; width:16px; height:16px; vertical-align:middle;"></span> ' + (STR.repairSearching || "") + '';
        }

        modalBody.innerHTML =
            '<div class="public-repairstatus__loading">' +
            '  <span class="select-loading" style="display:inline-block; width:22px; height:22px;"></span>' +
            '  ' + (STR.repairSearchingStatus || "") +
            '</div>';
        bootstrap.Modal.getOrCreateInstance(modalEl).show();

        fetch(lookupUrl + "?jobCode=" + encodeURIComponent(jobCode), {
            method: "GET",
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
        .then(function (res) {
            if (!res.ok) { throw new Error("lookup failed"); }
            return res.text();
        })
        .then(function (html) {
            modalBody.innerHTML = html;
        })
        .catch(function () {
            bootstrap.Modal.getInstance(modalEl).hide();
            showToast(STR.connectionError || "", "error");
        })
        .finally(function () {
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalBtnText;
            }
        });

        return true;
    }

    // Desktop sidebar collapse — initialized immediately (script runs at end of
    // body). The persisted state is also pre-applied in <head> to avoid a
    // layout flash, so we just read the current class and toggle it from here.
    (function () {
        var root = document.documentElement;
        var toggle = document.querySelector(".public-sidebar-toggle");
        if (!toggle) { return; }

        var KEY = "public.sidebarCollapsed";
        var collapsed = root.classList.contains("sidebar-collapsed");

        function apply(state) {
            root.classList.toggle("sidebar-collapsed", state);
            toggle.setAttribute("aria-expanded", state ? "false" : "true");
        }

        toggle.addEventListener("click", function () {
            collapsed = !collapsed;
            apply(collapsed);
            try { localStorage.setItem(KEY, collapsed ? "1" : "0"); } catch (e) { }
        });
    })();

    ready(function () {
        var hamburger = document.querySelector(".public-hamburger");
        var nav = document.querySelector(".public-nav");
        var backdrop = document.querySelector(".public-nav-backdrop");

        function toggleNav(open) {
            if (!hamburger || !nav || !backdrop) { return; }
            var isOpen = open === undefined ? !(nav.classList.contains("public-nav--open")) : open;
            nav.classList.toggle("public-nav--open", isOpen);
            if (isOpen) {
                backdrop.hidden = false;
                // Force layout reflow
                backdrop.offsetHeight;
                backdrop.classList.add("public-nav-backdrop--open");
            } else {
                backdrop.classList.remove("public-nav-backdrop--open");
                var onTransitionEnd = function () {
                    if (!backdrop.classList.contains("public-nav-backdrop--open")) {
                        backdrop.hidden = true;
                    }
                    backdrop.removeEventListener("transitionend", onTransitionEnd);
                };
                backdrop.addEventListener("transitionend", onTransitionEnd);
            }
            hamburger.setAttribute("aria-expanded", isOpen ? "true" : "false");
            document.body.style.overflow = isOpen ? "hidden" : "";
        }

        if (hamburger) {
            hamburger.addEventListener("click", function () { toggleNav(); });
        }
        if (backdrop) {
            backdrop.addEventListener("click", function () { toggleNav(false); });
        }
        if (nav) {
            nav.querySelectorAll("a").forEach(function (link) {
                link.addEventListener("click", function () { toggleNav(false); });
            });
        }
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") { toggleNav(false); }
        });

        // Global Event Interceptor for AJAX actions
        document.addEventListener("submit", function (e) {
            var form = e.target;

            // 1. Add to Cart Intercept
            if (form.hasAttribute("data-cart-add")) {
                e.preventDefault();
                submitCartAdd(form);
            }

            // 2. Quantity Update Intercept on Cart Page
            else if (form.classList.contains("public-cart-item__qty")) {
                e.preventDefault();
                var newQty = 1;
                if (e.submitter && e.submitter.name === "quantity") {
                    newQty = parseInt(e.submitter.value, 10) || 1;
                } else {
                    var input = form.querySelector('input[name="quantityDisplay"]');
                    newQty = parseInt(input ? input.value : "1", 10) || 1;
                }
                updateCartItemQty(form, newQty);
            }

            // 3. Remove Item Intercept on Cart Page
            else if (form.getAttribute("action") && form.getAttribute("action").indexOf("/Cart/Remove") !== -1) {
                e.preventDefault();
                removeCartItem(form);
            }

            // 4. Repair Status Lookup Intercept on Landing Page (opens modal)
            else if (form.hasAttribute("data-repair-status-lookup")) {
                if (lookupRepairStatus(form)) {
                    e.preventDefault();
                }
            }
        });

        // Hero slider autoplay + dot navigation
        var heroSlider = document.querySelector("[data-hero-slider]");
        if (heroSlider) {
            var heroSlides = heroSlider.querySelectorAll(".public-hero-slide");
            var heroDots = heroSlider.querySelectorAll("[data-hero-dot]");
            var heroCurrent = 0;
            var heroTimer = null;

            function showHeroSlide(index) {
                if (index < 0 || index >= heroSlides.length) { return; }
                heroSlides.forEach(function (s) { s.classList.remove("is-active"); });
                heroDots.forEach(function (d) { d.classList.remove("is-active"); });
                heroSlides[index].classList.add("is-active");
                if (heroDots[index]) { heroDots[index].classList.add("is-active"); }
                heroCurrent = index;
            }

            function advanceHeroSlide() {
                if (heroSlides.length <= 1) { return; }
                showHeroSlide((heroCurrent + 1) % heroSlides.length);
            }

            function restartHeroTimer() {
                if (heroTimer) { clearInterval(heroTimer); }
                if (heroSlides.length > 1) {
                    heroTimer = setInterval(advanceHeroSlide, 5000);
                }
            }

            heroDots.forEach(function (dot) {
                dot.addEventListener("click", function () {
                    showHeroSlide(parseInt(dot.getAttribute("data-index"), 10) || 0);
                    restartHeroTimer();
                });
            });

            // Swipe (touch) + click-drag (mouse) navigation
            var dragStartX = 0;
            var dragActive = false;
            var dragMoved = false;

            function heroSlideAt(offset) {
                var count = heroSlides.length;
                return ((heroCurrent + offset) % count + count) % count;
            }

            var heroPrev = heroSlider.querySelector("[data-hero-prev]");
            var heroNext = heroSlider.querySelector("[data-hero-next]");
            if (heroPrev) {
                heroPrev.addEventListener("click", function () {
                    showHeroSlide(heroSlideAt(-1));
                    restartHeroTimer();
                });
            }
            if (heroNext) {
                heroNext.addEventListener("click", function () {
                    showHeroSlide(heroSlideAt(1));
                    restartHeroTimer();
                });
            }

            heroSlider.addEventListener("dragstart", function (e) {
                e.preventDefault();
            });

            heroSlider.addEventListener("pointerdown", function (e) {
                if (heroSlides.length <= 1) { return; }
                if (e.target.closest("[data-hero-prev], [data-hero-next], [data-hero-dot]")) { return; }
                dragActive = true;
                dragMoved = false;
                dragStartX = e.clientX;
                if (heroTimer) { clearInterval(heroTimer); }
                if (heroSlider.setPointerCapture) { heroSlider.setPointerCapture(e.pointerId); }
            });

            heroSlider.addEventListener("pointermove", function (e) {
                if (!dragActive) { return; }
                if (Math.abs(e.clientX - dragStartX) > 10) { dragMoved = true; }
            });

            function endHeroDrag(e) {
                if (!dragActive) { return; }
                dragActive = false;
                var dx = e.clientX - dragStartX;
                if (Math.abs(dx) >= 40) {
                    showHeroSlide(heroSlideAt(dx < 0 ? 1 : -1));
                }
                restartHeroTimer();
            }

            heroSlider.addEventListener("pointerup", endHeroDrag);
            heroSlider.addEventListener("pointercancel", function () {
                dragActive = false;
                restartHeroTimer();
            });

            // Swallow clicks that follow a drag so the CTA link isn't triggered
            heroSlider.addEventListener("click", function (e) {
                if (dragMoved) {
                    e.preventDefault();
                    e.stopPropagation();
                    dragMoved = false;
                }
            }, true);

            restartHeroTimer();
        }

        // Floating contact button toggle
        var contactToggle = document.querySelector("[data-contact-toggle]");
        if (contactToggle) {
            var contactMenu = document.querySelector("[data-contact-menu]");
            function setContactOpen(open) {
                contactToggle.setAttribute("aria-expanded", open ? "true" : "false");
                if (contactMenu) { contactMenu.hidden = !open; }
            }
            contactToggle.addEventListener("click", function (e) {
                e.stopPropagation();
                setContactOpen(contactToggle.getAttribute("aria-expanded") !== "true");
            });
            document.addEventListener("click", function (e) {
                if (!e.target.closest(".public-contact-float")) {
                    setContactOpen(false);
                }
            });
        }
    });
})();
