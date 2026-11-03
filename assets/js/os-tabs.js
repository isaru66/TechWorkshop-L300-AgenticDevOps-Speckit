(() => {
  "use strict";

  const icons = document.getElementById("code-copy-icons");
  if (icons) document.body.append(icons.content.cloneNode(true));

  document.querySelectorAll(".main-content pre").forEach((pre, index) => {
    const code = pre.querySelector("code") || pre;
    let block = pre.closest("div.highlighter-rouge, figure.highlight, div.listingblock > div.content");
    if (!block) {
      block = document.createElement("div");
      pre.before(block);
      block.append(pre);
    }
    block.classList.add("code-copy-block");
    const controls = document.createElement("div");
    controls.className = "code-copy-feedback";
    const copy = document.createElement("button");
    copy.type = "button";
    copy.className = "code-copy-button";
    copy.title = "Copy code";
    copy.innerHTML = '<svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><use href="#svg-copy"></use></svg>';
    copy.setAttribute("aria-label", `Copy code block ${index + 1}`);
    const icon = copy.querySelector("use");
    let resetTimer;
    const status = document.createElement("span");
    status.className = "code-copy-status";
    status.setAttribute("role", "status");
    status.setAttribute("aria-live", "polite");
    const manual = document.createElement("button");
    manual.type = "button";
    manual.textContent = "Select code";
    manual.hidden = true;
    manual.addEventListener("click", () => {
      const selection = window.getSelection();
      const range = document.createRange();
      range.selectNodeContents(code);
      pre.tabIndex = -1;
      pre.focus();
      selection.removeAllRanges();
      selection.addRange(range);
      status.textContent = "Code selected. Press Ctrl+C (Windows/Linux) or Command+C (macOS).";
    });
    copy.addEventListener("click", async () => {
      clearTimeout(resetTimer);
      copy.disabled = true;
      icon.setAttribute("href", "#svg-copy");
      status.textContent = "Copying...";
      manual.hidden = true;
      controls.classList.remove("is-error");
      try {
        if (!window.isSecureContext || !navigator.clipboard?.writeText) {
          throw new Error("Clipboard access requires HTTPS or localhost and a supported browser.");
        }
        await navigator.clipboard.writeText(code.textContent);
        status.textContent = "Copied!";
        copy.title = "Copied!";
        icon.setAttribute("href", "#svg-copied");
        resetTimer = setTimeout(() => {
          icon.setAttribute("href", "#svg-copy");
          copy.title = "Copy code";
        }, 2000);
      } catch (error) {
        console.warn("Could not copy code to the clipboard.", error);
        copy.title = "Copy failed";
        status.textContent = "Copy failed. Use Select code, then copy with your keyboard.";
        controls.classList.add("is-error");
        manual.hidden = false;
      } finally {
        copy.disabled = false;
      }
    });
    controls.append(status, manual);
    block.append(copy);
    block.after(controls);
  });

  const groups = Array.from(document.querySelectorAll("[data-os-tabs]"));
  if (!groups.length) return;

  const platforms = ["windows", "macos", "linux"];
  const storageKey = "workshop-os";
  const persistenceWarning = "OS selection could not be saved. Tabs still synchronize on this page.";
  let storageAvailable = true;

  function detectPlatform() {
    const platform = navigator.userAgentData?.platform || navigator.platform || navigator.userAgent;
    if (/mac/i.test(platform)) return "macos";
    if (/linux/i.test(platform)) return "linux";
    return "windows";
  }

  let selected = detectPlatform();
  try {
    const saved = localStorage.getItem(storageKey);
    if (platforms.includes(saved)) selected = saved;
  } catch (error) {
    storageAvailable = false;
    console.warn(persistenceWarning, error);
  }

  function select(platform, source) {
    selected = platform;
    groups.forEach(group => {
      group.querySelectorAll('[role="tab"]').forEach(tab => {
        const active = tab.dataset.os === platform;
        tab.setAttribute("aria-selected", String(active));
        tab.tabIndex = active ? 0 : -1;
      });
      group.querySelectorAll("[data-os-panel]").forEach(panel => {
        panel.hidden = panel.dataset.osPanel !== platform;
      });
    });

    if (!source) return;
    try {
      localStorage.setItem(storageKey, platform);
      storageAvailable = true;
    } catch (error) {
      storageAvailable = false;
      console.warn(persistenceWarning, error);
    }
    source.querySelector(".os-tabs__status").textContent = storageAvailable ? "" : persistenceWarning;
  }

  groups.forEach(group => {
    const tablist = group.querySelector('[role="tablist"]');
    const tabs = Array.from(tablist.querySelectorAll('[role="tab"]'));
    tabs.forEach((tab, index) => {
      const panel = document.getElementById(tab.getAttribute("aria-controls"));
      panel.setAttribute("role", "tabpanel");
      panel.setAttribute("aria-labelledby", tab.id);
      panel.tabIndex = 0;
      panel.querySelector(".os-tabs__heading").hidden = true;

      tab.addEventListener("click", () => select(tab.dataset.os, group));
      tab.addEventListener("keydown", event => {
        let next;
        if (event.key === "ArrowRight") next = (index + 1) % tabs.length;
        else if (event.key === "ArrowLeft") next = (index + tabs.length - 1) % tabs.length;
        else if (event.key === "Home") next = 0;
        else if (event.key === "End") next = tabs.length - 1;
        else return;
        event.preventDefault();
        select(tabs[next].dataset.os, group);
        tabs[next].focus();
      });
    });

    tablist.hidden = false;
    group.classList.add("os-tabs--ready");
    if (!storageAvailable) group.querySelector(".os-tabs__status").textContent = persistenceWarning;
  });

  select(selected);
})();
