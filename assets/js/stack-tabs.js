(() => {
  "use strict";

  const groups = Array.from(document.querySelectorAll("[data-stack-tabs]")).map(group => {
    const tablist = group.querySelector(".stack-tabs__list");
    const tabs = Array.from(tablist.querySelectorAll('[role="tab"]'));
    const panels = tabs.map(tab => document.getElementById(tab.getAttribute("aria-controls")));
    return { group, tablist, tabs, panels };
  });
  if (!groups.length) return;

  const stacks = ["java", "dotnet"];
  const storageKey = "workshop-stack";
  const persistenceWarning = "Stack selection could not be saved. Tabs still synchronize on this page.";
  let storageAvailable = true;
  let selected = "java";

  try {
    const saved = localStorage.getItem(storageKey);
    if (stacks.includes(saved)) selected = saved;
  } catch (error) {
    storageAvailable = false;
    console.warn(persistenceWarning, error);
  }

  function select(stack, source) {
    if (!stacks.includes(stack)) return;

    groups.forEach(({ tabs, panels }) => {
      tabs.forEach((tab, index) => {
        const active = tab.dataset.stack === stack;
        tab.setAttribute("aria-selected", String(active));
        tab.tabIndex = active ? 0 : -1;
        panels[index].hidden = !active;
      });
    });

    if (!source) return;
    try {
      localStorage.setItem(storageKey, stack);
      storageAvailable = true;
    } catch (error) {
      storageAvailable = false;
      console.warn(persistenceWarning, error);
    }

    groups.forEach(({ group }) => {
      group.querySelector(".stack-tabs__status").textContent =
        !storageAvailable && group === source ? persistenceWarning : "";
    });
  }

  groups.forEach(({ group, tablist, tabs, panels }) => {
    tabs.forEach((tab, index) => {
      const panel = panels[index];
      panel.setAttribute("role", "tabpanel");
      panel.setAttribute("aria-labelledby", tab.id);
      panel.tabIndex = 0;
      panel.querySelector(".stack-tabs__heading").hidden = true;

      tab.addEventListener("click", () => select(tab.dataset.stack, group));
      tab.addEventListener("keydown", event => {
        let next;
        if (event.key === "ArrowRight") next = (index + 1) % tabs.length;
        else if (event.key === "ArrowLeft") next = (index + tabs.length - 1) % tabs.length;
        else if (event.key === "Home") next = 0;
        else if (event.key === "End") next = tabs.length - 1;
        else return;
        event.preventDefault();
        select(tabs[next].dataset.stack, group);
        tabs[next].focus();
      });
    });

    group.classList.add("stack-tabs--ready");
    if (!storageAvailable) group.querySelector(".stack-tabs__status").textContent = persistenceWarning;
  });

  select(selected);
  groups.forEach(({ tablist }) => {
    tablist.hidden = false;
  });
})();
