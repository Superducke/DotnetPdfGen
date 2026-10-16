"use strict";

const $ = (id) => document.getElementById(id);

const editors = { html: $("editor-html"), css: $("editor-css"), data: $("editor-data") };
const pdfFrame = $("pdf-frame");
const htmlFrame = $("html-frame");
const statusEl = $("status");
const downloadBtn = $("download");

const BLANK = {
  html: "<h1>Hello, {{Name}}!</h1>\n<p>Edit the HTML, CSS and JSON data to see the PDF update.</p>\n",
  css: "body { font-family: Arial, sans-serif; }\nh1 { color: #1f4e79; }\n",
  data: { Name: "World" },
};

let view = "pdf";
let pdfUrl = null;
let fileName = "document.pdf";
let renderSeq = 0;
let debounceTimer = 0;

function setStatus(text, kind = "") {
  statusEl.textContent = text;
  statusEl.className = `status ${kind}`;
}

function loadIntoEditors({ html, css, data }) {
  editors.html.value = html ?? "";
  editors.css.value = css ?? "";
  editors.data.value = JSON.stringify(data ?? {}, null, 2);
}

function buildRequest() {
  let data;
  try {
    data = editors.data.value.trim() ? JSON.parse(editors.data.value) : null;
  } catch (e) {
    throw new Error(`Data is not valid JSON: ${e.message}`);
  }
  return {
    html: editors.html.value,
    css: editors.css.value || null,
    data,
    page: {
      size: $("page-size").value,
      orientation: $("orientation").value,
      marginMm: Number($("margin").value) || 0,
    },
  };
}

async function describeError(response) {
  try {
    const problem = await response.json();
    const details = problem.errors
      ? Object.entries(problem.errors).map(([k, v]) => `${k}: ${[].concat(v).join(" ")}`).join("\n")
      : problem.detail;
    return [problem.title, details].filter(Boolean).join("\n");
  } catch {
    return `${response.status} ${response.statusText}`;
  }
}

async function render() {
  const seq = ++renderSeq;
  let request;
  try {
    request = buildRequest();
  } catch (e) {
    setStatus(e.message, "error");
    return;
  }

  setStatus("Rendering…", "busy");
  const endpoint = view === "pdf" ? "/api/pdf" : "/api/pdf/html";
  let response;
  try {
    response = await fetch(endpoint, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
    });
  } catch (e) {
    if (seq === renderSeq) setStatus(`Request failed: ${e.message}`, "error");
    return;
  }

  // A newer render started while this one was in flight; drop the stale result.
  if (seq !== renderSeq) return;

  if (!response.ok) {
    setStatus(await describeError(response), "error");
    return;
  }

  $("empty").hidden = true;
  if (view === "pdf") {
    const blob = await response.blob();
    if (pdfUrl) URL.revokeObjectURL(pdfUrl);
    pdfUrl = URL.createObjectURL(blob);
    pdfFrame.src = pdfUrl;
    downloadBtn.disabled = false;
    const ms = response.headers.get("X-Render-Time-Ms");
    setStatus(`Rendered ${(blob.size / 1024).toFixed(1)} KB${ms ? ` in ${ms} ms` : ""}`, "ok");
  } else {
    htmlFrame.srcdoc = await response.text();
    setStatus("Showing the bound HTML that is sent to the renderer.", "ok");
  }
}

function scheduleRender() {
  if (!$("auto").checked) return;
  clearTimeout(debounceTimer);
  debounceTimer = setTimeout(render, 600);
}

async function selectSample(name) {
  if (!name) {
    loadIntoEditors(BLANK);
    $("description").textContent = "";
    fileName = "document.pdf";
    return render();
  }
  setStatus("Loading template…", "busy");
  const response = await fetch(`/api/samples/${encodeURIComponent(name)}`);
  if (!response.ok) {
    setStatus(await describeError(response), "error");
    return;
  }
  const sample = await response.json();
  loadIntoEditors(sample);
  $("description").textContent = sample.description;
  fileName = `${sample.name}.pdf`;
  return render();
}

function showTab(group, key) {
  document.querySelectorAll(`.tab[data-${group}]`).forEach((tab) => {
    const active = tab.dataset[group] === key;
    tab.classList.toggle("active", active);
    tab.setAttribute("aria-selected", String(active));
  });
}

document.querySelectorAll(".tab[data-editor]").forEach((tab) =>
  tab.addEventListener("click", () => {
    const key = tab.dataset.editor;
    showTab("editor", key);
    Object.entries(editors).forEach(([name, el]) => (el.hidden = name !== key));
    editors[key].focus();
  }));

document.querySelectorAll(".tab[data-view]").forEach((tab) =>
  tab.addEventListener("click", () => {
    view = tab.dataset.view;
    showTab("view", view);
    pdfFrame.hidden = view !== "pdf";
    htmlFrame.hidden = view !== "html";
    render();
  }));

Object.values(editors).forEach((el) => {
  el.addEventListener("input", scheduleRender);
  // Insert two spaces on Tab instead of moving focus; Shift+Tab still leaves the editor.
  el.addEventListener("keydown", (e) => {
    if (e.key !== "Tab" || e.shiftKey) return;
    e.preventDefault();
    el.setRangeText("  ", el.selectionStart, el.selectionEnd, "end");
    scheduleRender();
  });
});

["page-size", "orientation", "margin"].forEach((id) => $(id).addEventListener("change", scheduleRender));
$("sample").addEventListener("change", (e) => selectSample(e.target.value));
$("render").addEventListener("click", render);
downloadBtn.addEventListener("click", () => {
  if (!pdfUrl) return;
  const a = document.createElement("a");
  a.href = pdfUrl;
  a.download = fileName;
  a.click();
});

(async function init() {
  try {
    const samples = await (await fetch("/api/samples")).json();
    for (const s of samples) {
      const option = new Option(s.title, s.name);
      option.title = s.description;
      $("sample").add(option);
    }
    const initial = samples.find((s) => s.name === "invoice")?.name ?? "";
    $("sample").value = initial;
    await selectSample(initial);
  } catch (e) {
    setStatus(`Could not load samples: ${e.message}`, "error");
    loadIntoEditors(BLANK);
  }
})();
