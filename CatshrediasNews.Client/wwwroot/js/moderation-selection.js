window.moderationSelection = {
  getSelection(sourceSelector) {
    const source = document.querySelector(sourceSelector);
    if (!source) return null;

    const sel = window.getSelection();
    if (!sel || sel.isCollapsed || sel.rangeCount === 0) return null;

    const range = sel.getRangeAt(0);
    if (!source.contains(range.commonAncestorContainer)) return null;

    const text = sel.toString().replace(/\s+/g, " ").trim();
    return text.length >= 3 ? text : null;
  },

  clearSelection() {
    window.getSelection()?.removeAllRanges();
  },
};
