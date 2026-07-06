(function () {
  "use strict";

  function ready(fn) {
    if (document.readyState !== "loading") {
      fn();
    } else {
      document.addEventListener("DOMContentLoaded", fn);
    }
  }

  ready(function () {
    var body = document.body;
    var hamburger = document.querySelector(".app-hamburger");
    var backdrop = document.querySelector(".app-drawer-backdrop");
    var sidebar = document.getElementById("app-sidebar");
    var edgeZone = document.querySelector(".app-drawer-edge");

    if (!hamburger || !backdrop || !sidebar) {
      return;
    }

    var isRtl = document.documentElement.dir === "rtl";

    var DRAGGING_CLASS = "is-drawer-dragging";
    var SNAPPING_CLASS = "is-sidebar-snapping";
    var OPEN_THRESHOLD = 0.3;
    var VELOCITY_THRESHOLD = 1.2;
    var DEAD_ZONE = 8;

    var isDragging = false;
    var dragTarget = null;
    var startX = 0;
    var startY = 0;
    var startProgress = 0;
    var progress = 0;
    var lastProgress = 0;
    var drawerWidth = 0;
    var velocitySamples = [];
    var isScrollCancelled = false;

    /* ---------- helpers ---------- */

    function isOpen() {
      return body.classList.contains("is-drawer-open");
    }

    function open() {
      body.classList.add("is-drawer-open");
      hamburger.setAttribute("aria-expanded", "true");
      hamburger.setAttribute("aria-label", "إغلاق القائمة");
    }

    function close() {
      body.classList.remove("is-drawer-open");
      hamburger.setAttribute("aria-expanded", "false");
      hamburger.setAttribute("aria-label", "فتح القائمة");
    }

    function getDrawerWidth() {
      return sidebar.getBoundingClientRect().width || 320;
    }

    function clamp(val, min, max) {
      return Math.max(min, Math.min(max, val));
    }

    function translateForProgress(p) {
      if (isRtl) {
        return ((1 - p) * 100).toFixed(2) + "%";
      }
      return ((p - 1) * 100).toFixed(2) + "%";
    }

    function drawerVelocity(velocityPxS) {
      return isRtl ? -velocityPxS : velocityPxS;
    }

    function computeProgress(rawDeltaX) {
      var delta;
      if (isRtl) {
        delta = -rawDeltaX / drawerWidth;
      } else {
        delta = rawDeltaX / drawerWidth;
      }
      return clamp(startProgress + delta, 0, 1);
    }

    function applyProgress(p) {
      progress = p;
      sidebar.style.transform = "translateX(" + translateForProgress(p) + ")";
      sidebar.style.visibility = "visible";
      backdrop.style.opacity = p.toFixed(2);
    }

    function addVelocitySample(x) {
      var now = performance.now();
      velocitySamples.push({ x: x, t: now });
      var cutoff = now - 100;
      while (velocitySamples.length > 1 && velocitySamples[0].t < cutoff) {
        velocitySamples.shift();
      }
    }

    function getVelocity() {
      if (velocitySamples.length < 2) return 0;
      var first = velocitySamples[0];
      var last = velocitySamples[velocitySamples.length - 1];
      var dt = last.t - first.t;
      if (dt === 0) return 0;
      var dx = last.x - first.x;
      return (dx / dt) * 1000;
    }

    function isDesktop() {
      return window.matchMedia("(min-width: 768px)").matches;
    }

    /* ---------- static interactions ---------- */

    hamburger.addEventListener("click", function (e) {
      e.preventDefault();
      if (isOpen()) {
        close();
      } else {
        open();
      }
    });

    backdrop.addEventListener("click", function () {
      close();
    });

    document.addEventListener("keydown", function (e) {
      if (e.key === "Escape" && isOpen()) {
        close();
      }
    });

    sidebar.querySelectorAll("a").forEach(function (link) {
      link.addEventListener("click", function () {
        if (isOpen()) {
          close();
        }
      });
    });

    /* ============================================================
       Touch-driven drawer drag system
       ============================================================ */

    function startDrag(target) {
      isDragging = true;
      dragTarget = target;
      progress = startProgress;
      lastProgress = progress;
      body.classList.add(DRAGGING_CLASS);
      body.classList.remove(SNAPPING_CLASS);
      body.classList.remove("is-drawer-open");
      hamburger.setAttribute("aria-expanded", "false");
      hamburger.setAttribute("aria-label", "فتح القائمة");
      sidebar.style.visibility = "visible";
      backdrop.style.opacity = progress.toFixed(2);
      backdrop.style.pointerEvents = "none";
    }

    function endDrag(snapTarget) {
      isDragging = false;
      dragTarget = null;
      velocitySamples = [];
      body.classList.remove(DRAGGING_CLASS);
      backdrop.style.pointerEvents = "";

      if (snapTarget === "open") {
        sidebar.style.transform = "";
        sidebar.style.visibility = "";
        backdrop.style.opacity = "";
        body.classList.add(SNAPPING_CLASS);
        open();
        sidebar.addEventListener("transitionend", function cleanup() {
          sidebar.removeEventListener("transitionend", cleanup);
          body.classList.remove(SNAPPING_CLASS);
        });
      } else if (progress > 0.01) {
        var snapStartTransform = translateForProgress(progress);
        sidebar.style.transform = "translateX(" + snapStartTransform + ")";
        sidebar.style.visibility = "visible";
        backdrop.style.opacity = progress.toFixed(2);
        body.classList.add(SNAPPING_CLASS);
        sidebar.getBoundingClientRect();
        sidebar.style.transform = "";
        backdrop.style.opacity = "";
        sidebar.addEventListener("transitionend", function cleanup() {
          sidebar.removeEventListener("transitionend", cleanup);
          body.classList.remove(SNAPPING_CLASS);
          sidebar.style.visibility = "";
          close();
        });
      } else {
        sidebar.style.transform = "";
        sidebar.style.visibility = "";
        backdrop.style.opacity = "";
        close();
      }
    }

    function onPointerDown(e) {
      if (isDragging) return;
      if (isDesktop()) return;

      if (e.target.closest(".app-drawer-edge")) {
        drawerWidth = getDrawerWidth();
        startX = e.clientX;
        startY = e.clientY;
        startProgress = 0;
        isScrollCancelled = false;
        velocitySamples = [];
        startDrag("edge");
        document.addEventListener("pointermove", onPointerMove, { passive: false });
        document.addEventListener("pointerup", onPointerUp);
        document.addEventListener("pointercancel", onPointerUp);
        e.preventDefault();
      } else if (e.target.closest("#app-sidebar")) {
        if (e.target.closest("a, button, input, select, textarea, [data-sidebar-toggle]")) return;
        drawerWidth = getDrawerWidth();
        startX = e.clientX;
        startY = e.clientY;
        startProgress = isOpen() ? 1 : 0;
        isScrollCancelled = false;
        velocitySamples = [];
        startDrag("sidebar");
        document.addEventListener("pointermove", onPointerMove, { passive: false });
        document.addEventListener("pointerup", onPointerUp);
        document.addEventListener("pointercancel", onPointerUp);
        e.preventDefault();
      }
    }

    function onPointerMove(e) {
      if (!isDragging) return;

      var dx = e.clientX - startX;
      var dy = e.clientY - startY;
      var absDx = Math.abs(dx);
      var absDy = Math.abs(dy);

      if (!isScrollCancelled && absDx < DEAD_ZONE && absDy < DEAD_ZONE + 4) {
        return;
      }
      if (dragTarget === "sidebar" && !isScrollCancelled && absDy > absDx) {
        isScrollCancelled = true;
        endDrag(progress > OPEN_THRESHOLD ? "open" : "close");
        return;
      }

      isScrollCancelled = true;
      e.preventDefault();

      addVelocitySample(e.clientX);
      applyProgress(computeProgress(dx));
    }

    function onPointerUp() {
      document.removeEventListener("pointermove", onPointerMove);
      document.removeEventListener("pointerup", onPointerUp);
      document.removeEventListener("pointercancel", onPointerUp);

      if (!isDragging) return;
      if (!isScrollCancelled) {
        endDrag(progress > OPEN_THRESHOLD ? "open" : "close");
        return;
      }

      var velocityPxS = getVelocity();
      var velocityProgress = drawerVelocity(velocityPxS) / drawerWidth;
      var snapTarget;

      if (dragTarget === "edge" && velocityProgress > VELOCITY_THRESHOLD) {
        snapTarget = "open";
      } else if (dragTarget === "sidebar" && velocityProgress < -VELOCITY_THRESHOLD) {
        snapTarget = "close";
      } else {
        snapTarget = progress > OPEN_THRESHOLD ? "open" : "close";
      }

      endDrag(snapTarget);
    }

    if (edgeZone) {
      edgeZone.addEventListener("pointerdown", onPointerDown);
    }
    sidebar.addEventListener("pointerdown", onPointerDown);

    /* ---------- desktop auto-close ---------- */

    var mq = window.matchMedia("(min-width: 768px)");
    function handleMq(e) {
      if (e.matches) {
        close();
      }
    }
    if (mq.addEventListener) {
      mq.addEventListener("change", handleMq);
    } else if (mq.addListener) {
      mq.addListener(handleMq);
    }

    /* ---------- Tom Select init ---------- */
    if (window.MimoShop && typeof window.MimoShop.initTomSelect === "function") {
      window.MimoShop.initTomSelect(document);
    } else if (window.TomSelect) {
      document.querySelectorAll(".ts-select").forEach(function (el) {
        if (el.tomselect) return;
        new TomSelect(el, {
          maxItems: 1,
          placeholder: el.getAttribute("data-placeholder") || "ابحث…",
          create: false,
          sortField: { field: "text", direction: "asc" },
          lock: false,
          allowEmptyOption: true
        });
      });
    }
  });
})();

  /* Confirm bar toggle */
  document.querySelectorAll("[data-confirm-trigger]").forEach(function (trigger) {
    var form = trigger.closest("form");
    var actions = trigger.closest(".primary-action__actions");
    var bar = form?.querySelector("[data-confirm-bar]");
    var cancel = bar?.querySelector("[data-confirm-cancel]");
    if (!actions || !bar || !cancel) return;

    trigger.addEventListener("click", function () {
      actions.style.display = "none";
      bar.classList.add("is-visible");
    });

    cancel.addEventListener("click", function () {
      bar.classList.remove("is-visible");
      actions.style.display = "";
    });
  });

  /* Desktop sidebar collapse (icons-only rail) */
  (function () {
    var STORAGE_KEY = "mimoShop.sidebarCollapsed";
    var COLLAPSED_CLASS = "is-sidebar-collapsed";

    function getStorage() {
      try {
        return window.localStorage;
      } catch (e) {
        return null;
      }
    }

    function isDesktop() {
      return window.matchMedia("(min-width: 768px)").matches;
    }

    function readPreference() {
      var store = getStorage();
      if (!store) return false;
      return store.getItem(STORAGE_KEY) === "1";
    }

    function writePreference(collapsed) {
      var store = getStorage();
      if (!store) return;
      try {
        store.setItem(STORAGE_KEY, collapsed ? "1" : "0");
      } catch (e) {
        /* ignore quota / privacy mode */
      }
    }

    function reflectAria(collapsed) {
      var btn = document.querySelector("[data-sidebar-toggle]");
      if (!btn) return;
      btn.setAttribute("aria-expanded", collapsed ? "false" : "true");
      btn.setAttribute("aria-label", collapsed ? "فتح القائمة" : "طيّ القائمة");
      btn.setAttribute("title", collapsed ? "فتح القائمة" : "طيّ القائمة");
    }

    function applyCollapsed(collapsed) {
      if (!isDesktop() && collapsed) {
        document.body.classList.remove(COLLAPSED_CLASS);
      } else {
        document.body.classList.toggle(COLLAPSED_CLASS, !!collapsed);
      }
      reflectAria(document.body.classList.contains(COLLAPSED_CLASS));
    }

    function decorateTitles() {
      document.querySelectorAll(".app-sidebar .nav-item").forEach(function (link) {
        if (link.getAttribute("title")) return;
        var label = link.querySelector(".nav-item__label");
        if (label && label.textContent) {
          link.setAttribute("title", label.textContent.trim());
        }
      });
    }

    if (readPreference() && isDesktop()) {
      applyCollapsed(true);
    }
    decorateTitles();

    document.addEventListener("click", function (e) {
      var btn = e.target.closest && e.target.closest("[data-sidebar-toggle]");
      if (!btn) return;
      if (!isDesktop()) return;
      e.preventDefault();
      var nowCollapsed = !document.body.classList.contains(COLLAPSED_CLASS);
      applyCollapsed(nowCollapsed);
      writePreference(nowCollapsed);
    });

    var mq = window.matchMedia("(min-width: 768px)");
    function handleMq(e) {
      if (e.matches) {
        if (readPreference()) {
          applyCollapsed(true);
        } else {
          reflectAria(document.body.classList.contains(COLLAPSED_CLASS));
        }
      } else {
        if (document.body.classList.contains(COLLAPSED_CLASS)) {
          document.body.classList.remove(COLLAPSED_CLASS);
          reflectAria(false);
        }
      }
    }
    if (mq.addEventListener) {
      mq.addEventListener("change", handleMq);
    } else if (mq.addListener) {
      mq.addListener(handleMq);
    }
})();

/* ============================================================
   Bootstrap modal helpers
   - Confirm-modal intercept for inline action button-forms
   - Generic openModal / submitModalForm for inline modals
     fetched into a shared host (#mimoModalHost).
   ============================================================ */
(function () {
  "use strict";

  var MimoShop = window.MimoShop = window.MimoShop || {};

  function ready(fn) {
    if (document.readyState !== "loading") fn();
    else document.addEventListener("DOMContentLoaded", fn);
  }

  /* ---- shared Tom Select helper (used by initial page load and
         by dynamically inserted modals). ---- */
  function initTomSelect(root) {
    if (!window.TomSelect) return;
    var els = (root || document).querySelectorAll(".ts-select");
    els.forEach(function (el) {
      if (el.tomselect) return;
      new TomSelect(el, {
        maxItems: 1,
        placeholder: el.getAttribute("data-placeholder") || "ابحث…",
        create: false,
        sortField: { field: "text", direction: "asc" },
        lock: false,
        allowEmptyOption: true
      });
    });
  }
  MimoShop.initTomSelect = initTomSelect;

  /* ---- Confirm modal (used by inline action button-forms) ---- */
  function showConfirmFor(form) {
    var modalEl = document.getElementById("mimo-confirm-modal");
    if (!modalEl || !window.bootstrap) {
      form.dataset.confirmSkip = "1";
      form.submit();
      return;
    }
    var titleEl = modalEl.querySelector("[data-confirm-title]");
    var bodyEl  = modalEl.querySelector("[data-confirm-body]");
    var title = form.getAttribute("data-confirm-title");
    var body  = form.getAttribute("data-confirm-body");
    if (title && titleEl) titleEl.textContent = title;
    if (body && bodyEl)   bodyEl.textContent = body;
    var okBtn = modalEl.querySelector("[data-confirm-ok]");
    if (okBtn) {
      var fresh = okBtn.cloneNode(true);
      okBtn.replaceWith(fresh);
      fresh.addEventListener("click", function () {
        form.dataset.confirmSkip = "1";
        bootstrap.Modal.getOrCreateInstance(modalEl).hide();
        form.submit();
      });
    }
    bootstrap.Modal.getOrCreateInstance(modalEl).show();
  }

  function initConfirmIntercepts() {
    document.querySelectorAll("form[data-confirm-modal]").forEach(function (form) {
      if (form.dataset.confirmBound === "1") return;
      form.dataset.confirmBound = "1";
      if (window.$ && typeof $(form).valid === "function") {
        form.addEventListener("submit", function (e) {
          if (form.dataset.confirmSkip === "1") return;
          form.dataset._wasValid = $(form).valid() ? "1" : "0";
        }, true);
      }
      form.addEventListener("submit", function (e) {
        if (form.dataset.confirmSkip === "1") return;
        if (form.dataset._wasValid === "0") return;
        e.preventDefault();
        if (window.$ && typeof $(form).valid === "function" && !$(form).valid()) return;
        showConfirmFor(form);
      });
    });
  }

  /* ---- Generic inline-modal mechanism ---- */

  var MODAL_URLS = {
    repairCreate:        "/RepairTickets/CreateModal",
    staffCreate:         "/Staff/CreateModal",
    staffEdit:           "/Staff/EditModal/{arg}",
    staffResetPassword:  "/Staff/ResetPasswordModal/{arg}",
    inventoryForm:       "/Inventory/FormModal/{arg}",
    changePassword:      "/Account/ChangePasswordModal",
    shopSettings:        "/ShopSettings/SettingsModal",
    categoriesImport:    "/Categories/ImportPreviewModal"
  };

  function resolveUrl(key, arg) {
    var template = MODAL_URLS[key];
    if (!template) return null;
    if (template.indexOf("{arg}") >= 0) {
      return template.replace("{arg}", encodeURIComponent(arg == null ? "" : String(arg)));
    }
    return template;
  }

  function getHost() {
    return document.getElementById("mimoModalHost");
  }

  function injectHtml(root, html) {
    var doc = new DOMParser().parseFromString(html, "text/html");
    var modalEl = null;
    var nodes = Array.from(doc.body.childNodes);
    for (var i = 0; i < nodes.length; i++) {
      var node = nodes[i];
      var imported = document.importNode(node, true);
      if (imported.nodeType === 1 && imported.classList && imported.classList.contains("modal")) {
        modalEl = imported;
      }
      root.appendChild(imported);
      if (imported.nodeType === 1 && imported.tagName === "SCRIPT" && !imported.src) {
        var fresh = document.createElement("script");
        fresh.textContent = imported.textContent;
        root.replaceChild(fresh, imported);
      }
    }
    return modalEl;
  }

  function parseUnobtrusive(root) {
    if (!window.$ || !window.$.validator || !window.$.validator.unobtrusive) return;
    $(root).find("form").each(function () {
      try { $(this).removeData("validator").removeData("unobtrusiveValidator"); } catch (e) {}
      $.validator.unobtrusive.parse(this);
    });
  }

  function focusFirstField(modalEl) {
    if (!modalEl) return;
    var target = modalEl.querySelector(
      "input:not([type=hidden]):not([disabled]):not([type=file]), select:not([disabled]), textarea:not([disabled])"
    );
    if (target) {
      try { target.focus({ preventScroll: true }); } catch (e) { target.focus(); }
    }
  }

  function showModalEl(modalEl) {
    if (!modalEl || !window.bootstrap) return null;
    parseUnobtrusive(modalEl);
    initTomSelect(modalEl);
    var inst = bootstrap.Modal.getOrCreateInstance(modalEl);
    inst.show();
    modalEl.addEventListener("shown.bs.modal", function once() {
      modalEl.removeEventListener("shown.bs.modal", once);
      focusFirstField(modalEl);
    });
    return modalEl;
  }

  function closeModalEl(modalEl) {
    if (!modalEl || !window.bootstrap) return;
    var inst = bootstrap.Modal.getInstance(modalEl);
    if (inst) inst.hide();
  }

  function openModal(key, arg) {
    var url = resolveUrl(key, arg);
    if (!url) return Promise.reject(new Error("Unknown modal key: " + key));

    var host = getHost();
    if (!host) return Promise.reject(new Error("Modal host (#mimoModalHost) is missing"));

    var existing = host.querySelector('[data-modal-key="' + key + '"]');
    if (existing) {
      return Promise.resolve(showModalEl(existing));
    }

    return fetch(url, {
      headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "text/html" },
      credentials: "same-origin"
    })
      .then(function (r) {
        if (r.status === 204) {
          showPageToast("لا يوجد شيء لفتحه الآن.", "info");
          return null;
        }
        if (!r.ok) throw new Error("Failed to load modal (HTTP " + r.status + ")");
        return r.text();
      })
      .then(function (html) {
        if (!html) return null;
        var modalEl = injectHtml(host, html);
        if (!modalEl) throw new Error("Modal HTML did not contain a .modal root");
        modalEl.setAttribute("data-modal-key", key);
        return showModalEl(modalEl);
      });
  }

  function reloadModal(key, message) {
    var url = resolveUrl(key);
    return fetch(url, {
      headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "text/html" },
      credentials: "same-origin"
    })
      .then(function (r) {
        if (!r.ok) throw new Error("Failed to reload modal (HTTP " + r.status + ")");
        return r.text();
      })
      .then(function (html) {
        var host = getHost();
        if (!host) return null;
        var existing = host.querySelector('[data-modal-key="' + key + '"]');
        if (existing) existing.parentNode.removeChild(existing);
        var modalEl = injectHtml(host, html);
        if (!modalEl) return null;
        modalEl.setAttribute("data-modal-key", key);
        showModalEl(modalEl);
        if (message) showToast(modalEl, message);
        return modalEl;
      });
  }

  function replaceModalBody(form, html) {
    var modalEl = form.closest("[data-modal-key]");
    if (!modalEl) return;
    var key = modalEl.getAttribute("data-modal-key");
    var host = getHost();
    if (!host) return;
    var probe = document.createElement("div");
    probe.innerHTML = html;
    var probeModal = null;
    for (var i = 0; i < probe.childNodes.length; i++) {
      var n = probe.childNodes[i];
      if (n.nodeType === 1 && n.classList && n.classList.contains("modal")) {
        probeModal = n;
        break;
      }
    }
    if (!probeModal) {
      showFormError(form, "تعذر تحميل النموذج. حاول مجدداً.");
      return;
    }
    modalEl.parentNode.removeChild(modalEl);
    var next = injectHtml(host, html);
    if (!next) return;
    next.setAttribute("data-modal-key", key);
    showModalEl(next);
  }

  function showToast(modalEl, message, kind) {
    if (!modalEl || !message) return;
    var body = modalEl.querySelector(".modal-body");
    if (!body) return;
    var old = modalEl.querySelector(".modal-toast");
    if (old) old.remove();
    var toast = document.createElement("div");
    toast.className = "alert " + (kind === "error" ? "alert-danger" : "alert-success") + " modal-toast";
    toast.setAttribute("role", kind === "error" ? "alert" : "status");
    toast.style.marginBottom = "var(--space-3)";
    toast.textContent = message;
    body.insertBefore(toast, body.firstChild);
  }

  function showPageToast(message, kind) {
    if (!message) return;
    var container = document.querySelector(".app-container .page") || document.body;
    var toast = document.createElement("div");
    var cls = "alert " + (kind === "error" ? "alert-danger" : kind === "info" ? "alert-info" : "alert-success");
    toast.className = cls + " page-toast";
    toast.setAttribute("role", kind === "error" ? "alert" : "status");
    toast.style.marginBottom = "var(--space-3)";
    toast.textContent = message;
    container.insertBefore(toast, container.firstChild);
    window.setTimeout(function () {
      if (toast.parentNode) toast.parentNode.removeChild(toast);
    }, 4500);
  }

  function showFormError(form, message) {
    var summary = form.querySelector(".validation-summary");
    if (summary) {
      summary.innerHTML = "<ul><li>" + escapeHtml(message) + "</li></ul>";
      summary.hidden = false;
    }
    var modalEl = form.closest("[data-modal-key]");
    showToast(modalEl, message, "error");
  }

  function escapeHtml(s) {
    return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
    });
  }

  function submitModalForm(form, e) {
    if (e && e.preventDefault) e.preventDefault();
    if (form.dataset.busy === "1") return Promise.resolve();
    form.dataset.busy = "1";

    var submitter = (e && e.submitter) || form.querySelector("button[type=submit]:focus") || null;
    var formData = new FormData(form);
    if (submitter && submitter.name && formData.get(submitter.name) == null) {
      formData.append(submitter.name, submitter.value || "");
    }

    var url = form.getAttribute("action") || window.location.href;
    return fetch(url, {
      method: (form.getAttribute("method") || "POST").toUpperCase(),
      body: formData,
      headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "application/json, text/html" },
      credentials: "same-origin"
    })
      .then(function (r) {
        var ct = r.headers.get("content-type") || "";
        if (ct.indexOf("application/json") >= 0) {
          return r.json().then(function (data) { return { kind: "json", status: r.status, ok: r.ok, data: data }; });
        }
        return r.text().then(function (t) { return { kind: "html", status: r.status, ok: r.ok, text: t }; });
      })
      .then(function (res) {
        form.dataset.busy = "";
        if (res.kind === "json") {
          return handleJsonResponse(form, res);
        }
        if (res.kind === "html" && res.ok) {
          return { reload: true };
        }
        if (res.kind === "html" && !res.ok) {
          replaceModalBody(form, res.text);
          return null;
        }
        throw new Error("Unexpected response");
      })
      .catch(function (err) {
        form.dataset.busy = "";
        showFormError(form, err && err.message ? err.message : "حدث خطأ غير متوقع.");
      });
  }

  function handleJsonResponse(form, res) {
    var data = res.data || {};
    if (res.ok) {
      if (data.redirectUrl) {
        var modalEl = form.closest("[data-modal-key]");
        if (modalEl) closeModalEl(modalEl);
        window.setTimeout(function () { window.location.href = data.redirectUrl; }, 150);
        return;
      }
      if (data.replace) {
        return reloadModal(data.replace, data.message);
      }
      if (data.reload) {
        var m2 = form.closest("[data-modal-key]");
        if (m2) closeModalEl(m2);
        window.setTimeout(function () { window.location.reload(); }, data.reloadDelay || 250);
        return;
      }
      if (data.message) {
        var m3 = form.closest("[data-modal-key]");
        showToast(m3, data.message);
      }
      return;
    }
    throw new Error(data.message || "Request failed");
  }

  /* ---- Wiring ---- */

  function initTriggers() {
    document.addEventListener("click", function (e) {
      var trigger = e.target.closest && e.target.closest("[data-open-modal]");
      if (!trigger) return;
      // Allow normal behavior for disabled controls / native elements that need it
      if (trigger.hasAttribute("disabled")) return;
      e.preventDefault();
      var key = trigger.getAttribute("data-open-modal");
      var arg = trigger.getAttribute("data-modal-arg");
      openModal(key, arg).catch(function (err) {
        console.error("openModal failed", err);
      });
    });

    document.addEventListener("submit", function (e) {
      var form = e.target.closest && e.target.closest("[data-modal-form]");
      if (!form) return;
      submitModalForm(form, e);
    });
  }

  MimoShop.modal = {
    showConfirmFor: showConfirmFor,
    initConfirmIntercepts: initConfirmIntercepts,
    openModal: openModal,
    closeModal: closeModalEl,
    submitForm: submitModalForm,
    reloadModal: reloadModal,
    urls: MODAL_URLS
  };

  ready(function () {
    initTriggers();
    initConfirmIntercepts();
  });

  ready(function () {
    initPriceInputs();
  });

  function initPriceInputs() {
    if (!document.querySelector) return;

    function sanitize(text) {
      var cleaned = String(text == null ? "" : text).replace(/[^0-9.,]/g, "");
      var firstDot = cleaned.indexOf(".");
      var firstComma = cleaned.indexOf(",");
      var decimals = [];
      if (firstDot !== -1) decimals.push(firstDot);
      if (firstComma !== -1) decimals.push(firstComma);
      if (decimals.length > 1) {
        var first = Math.min.apply(null, decimals);
        var parts = [cleaned.slice(0, first)];
        parts.push(cleaned.slice(first + 1).replace(/[.,]/g, ""));
        cleaned = parts.join(cleaned.charAt(first));
      } else if (decimals.length === 1) {
        var idx = decimals[0];
        cleaned = cleaned.slice(0, idx + 1) + cleaned.slice(idx + 1).replace(/[.,]/g, "");
      } else {
        cleaned = cleaned.replace(/[.,]/g, "");
      }
      return cleaned;
    }

    function attachTo(input) {
      if (!input || input.dataset.priceInputBound === "1") return;
      input.dataset.priceInputBound = "1";

      input.addEventListener("input", function () {
        var before = input.value;
        var after = sanitize(before);
        if (after !== before) {
          var pos = input.selectionStart;
          input.value = after;
          if (typeof pos === "number") {
            try { input.setSelectionRange(after.length, after.length); } catch (e) { /* ignore */ }
          }
        }
      });
    }

    function bindAll() {
      var nodes = document.querySelectorAll("[data-price-input]");
      for (var i = 0; i < nodes.length; i++) attachTo(nodes[i]);
    }

    document.addEventListener("focusin", function (e) {
      var t = e.target;
      if (!t || !t.matches || !t.matches("[data-price-input]")) return;
      if (t.readOnly || t.disabled) return;
      var val = t.value;
      if (val && val.length > 0) {
        try {
          t.setSelectionRange(val.length, val.length);
          window.setTimeout(function () {
            if (document.activeElement === t) {
              try { t.setSelectionRange(0, val.length); } catch (e) { /* ignore */ }
            }
          }, 0);
        } catch (err) { /* ignore */ }
      }
    });

    document.addEventListener("click", function (e) {
      var btn = e.target.closest && e.target.closest("[data-price-clear]");
      if (!btn) return;
      e.preventDefault();
      var input = btn.parentNode && btn.parentNode.querySelector("[data-price-input]");
      if (!input) return;
      input.value = "";
      try { input.focus(); } catch (err) { /* ignore */ }
      try { input.dispatchEvent(new Event("input", { bubbles: true })); } catch (err) { /* ignore */ }
    });

    bindAll();

    var observer = null;
    if (typeof MutationObserver === "function" && document.body) {
      observer = new MutationObserver(function () { bindAll(); });
      observer.observe(document.body, { childList: true, subtree: true });
    }
  }
})();
