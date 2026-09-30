(function () {
  "use strict";

  var LS_PREFIX = "mimo.tutorial.seen.";
  var STEP_DIR = "/js/tutorials/";

  var registry = {};
  var loadedKeys = {};
  var loadingKeys = {};
  var activeDriver = null;

  function routeKey() {
    return window.MimoShop && window.MimoShop.tutorials
      ? window.MimoShop.tutorials.currentKey
      : null;
  }

  function isSeen(key) {
    try {
      return localStorage.getItem(LS_PREFIX + key) === "1";
    } catch (e) {
      return false;
    }
  }

  function markSeen(key) {
    try {
      localStorage.setItem(LS_PREFIX + key, "1");
    } catch (e) {}
  }

  function clearSeen(key) {
    try {
      localStorage.removeItem(LS_PREFIX + key);
    } catch (e) {}
  }

  function declare(key, steps) {
    registry[key] = steps;
    loadedKeys[key] = true;
  }

  function loadSteps(key) {
    if (loadedKeys[key]) {
      return Promise.resolve(registry[key]);
    }
    if (loadingKeys[key]) {
      return loadingKeys[key];
    }
    var url = STEP_DIR + key + ".js";
    loadingKeys[key] = fetch(url, { cache: "no-cache" })
      .then(function (resp) {
        if (!resp.ok) {
          throw new Error("HTTP " + resp.status);
        }
        return resp.text();
      })
      .then(function (text) {
        var fn = new Function("MimoShop", text);
        fn(window.MimoShop);
        return registry[key];
      })
      .catch(function (err) {
        console.warn("[tutorials] Failed to load steps for '" + key + "':", err);
        loadingKeys[key] = null;
        return null;
      });
    return loadingKeys[key];
  }

  function resolveElement(step) {
    if (!step.element) {
      return null;
    }
    if (typeof step.element === "string") {
      return document.querySelector(step.element);
    }
    return step.element;
  }

  function toDriverStep(step, index, total) {
    var el = resolveElement(step);
    var ds = {
      popover: {
        title: step.title || "",
        description: step.body || "",
        side: step.side || "bottom",
        align: step.align || "start",
      },
    };
    if (el) {
      ds.element = el;
    }
    return ds;
  }

  function start(key) {
    if (activeDriver) {
      try { activeDriver.destroy(); } catch (e) {}
      activeDriver = null;
    }
    if (!key) {
      key = routeKey();
    }
    if (!key) {
      console.warn("[tutorials] No tutorial key for this page.");
      return;
    }
    loadSteps(key).then(function (steps) {
      if (!steps || !steps.length) {
        console.warn("[tutorials] No steps declared for '" + key + "'.");
        return;
      }
      var driverSteps = steps.map(toDriverStep);
      var ns = (window.driver && window.driver.js) ? window.driver.js : window.driver;
      var DriverCtor = ns ? (ns.Driver || ns.driver) : null;
      if (!DriverCtor) {
        console.warn("[tutorials] driver.js library not loaded.");
        return;
      }
      var driver = new DriverCtor({
        showProgress: true,
        progressText: " الخطوة {{current}} من {{total}} ",
        nextBtnText: "التالي &rsaquo;",
        prevBtnText: "&lsaquo; السابق",
        doneBtnText: "تم",
        animate: true,
        allowClose: true,
        smoothScroll: true,
        stagePadding: 8,
        stageRadius: 6,
        popoverClass: "mimo-tutorial-popover",
        steps: driverSteps,
        onDestroyed: function () {
          activeDriver = null;
          markSeen(key);
        },
      });
      activeDriver = driver;
      driver.drive();
    });
  }

  function reset(key) {
    if (!key) {
      key = routeKey();
    }
    if (key) {
      clearSeen(key);
    }
  }

  window.MimoShop = window.MimoShop || {};
  window.MimoShop.tutorials = {
    currentKey: null,
    declare: declare,
    start: start,
    reset: reset,
    isSeen: isSeen,
  };
})();
