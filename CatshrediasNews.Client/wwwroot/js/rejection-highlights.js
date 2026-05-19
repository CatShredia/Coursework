window.rejectionHighlights = {
  highlight(containerSelector, notes) {
    const container = document.querySelector(containerSelector);
    if (!container) return [];

    this.clear(container);
    const notFound = [];

    for (const note of notes || []) {
      const excerpt = (note.excerpt || "").trim();
      if (!excerpt) continue;
      if (!this._highlightFirst(container, excerpt, note.reason || ""))
        notFound.push(excerpt);
    }

    return notFound;
  },

  clear(containerOrSelector) {
    const container =
      typeof containerOrSelector === "string"
        ? document.querySelector(containerOrSelector)
        : containerOrSelector;
    if (!container) return;

    container.querySelectorAll("mark.rejection-highlight").forEach((mark) => {
      const text = document.createTextNode(mark.textContent);
      mark.replaceWith(text);
    });
    container.normalize();
  },

  _highlightFirst(root, search, title) {
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    let node;

    while ((node = walker.nextNode())) {
      const text = node.textContent;
      if (!text) continue;

      let idx = text.indexOf(search);
      if (idx < 0) {
        const collapsed = text.replace(/\s+/g, " ");
        const collapsedSearch = search.replace(/\s+/g, " ");
        idx = collapsed.indexOf(collapsedSearch);
        if (idx < 0) continue;
        search = collapsedSearch;
      }

      const range = document.createRange();
      range.setStart(node, idx);
      range.setEnd(node, Math.min(idx + search.length, text.length));

      const mark = document.createElement("mark");
      mark.className = "rejection-highlight";
      if (title) mark.title = title;

      try {
        range.surroundContents(mark);
        return true;
      } catch {
        const frag = range.extractContents();
        mark.appendChild(frag);
        range.insertNode(mark);
        return true;
      }
    }

    return false;
  },
};
