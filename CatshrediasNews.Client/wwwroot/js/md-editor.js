window.mdEditor = {

    wrap(id, before, after, placeholder) {
        const ta = document.getElementById(id);
        if (!ta) return '';
        const start = ta.selectionStart;
        const end   = ta.selectionEnd;
        const sel   = ta.value.substring(start, end) || placeholder;
        const val   = ta.value.substring(0, start) + before + sel + after + ta.value.substring(end);
        ta.value    = val;
        ta.focus();
        ta.setSelectionRange(start + before.length, start + before.length + sel.length);
        return val;
    },

    linePrefix(id, prefix) {
        const ta        = document.getElementById(id);
        if (!ta) return '';
        const start     = ta.selectionStart;
        const lineStart = ta.value.lastIndexOf('\n', start - 1) + 1;
        const val       = ta.value.substring(0, lineStart) + prefix + ta.value.substring(lineStart);
        ta.value        = val;
        ta.focus();
        ta.setSelectionRange(start + prefix.length, start + prefix.length);
        return val;
    },

    insert(id, text) {
        const ta  = document.getElementById(id);
        if (!ta) return '';
        const pos = ta.selectionStart;
        const val = ta.value.substring(0, pos) + text + ta.value.substring(pos);
        ta.value  = val;
        ta.focus();
        ta.setSelectionRange(pos + text.length, pos + text.length);
        return val;
    }
};
