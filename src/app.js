(() => {
  const { LzxError, convertFileBytes } = globalThis.X68000Lzx;

  const fileInput = document.querySelector("#file-input");
  const downloadLink = document.querySelector("#download-link");
  const selectedFile = document.querySelector("#selected-file");
  const status = document.querySelector("#status");
  const dropZone = document.querySelector("#drop-zone");

  let currentFile = null;
  let currentObjectUrl = null;

  fileInput.addEventListener("change", () => {
    setFile(fileInput.files?.[0] ?? null);
  });

  dropZone.addEventListener("dragover", (event) => {
    event.preventDefault();
    dropZone.classList.add("dragging");
  });

  dropZone.addEventListener("dragleave", () => {
    dropZone.classList.remove("dragging");
  });

  dropZone.addEventListener("drop", (event) => {
    event.preventDefault();
    dropZone.classList.remove("dragging");
    setFile(event.dataTransfer.files?.[0] ?? null);
  });

  async function convertCurrentFile() {
    if (!currentFile) {
      return;
    }

    revokeCurrentUrl();
    setStatus(`読み込み中: ${currentFile.name}...`);

    try {
      const buffer = await currentFile.arrayBuffer();
      const result = convertFileBytes(buffer, currentFile.name);
      const blob = new Blob([result.bytes], { type: "application/octet-stream" });

      currentObjectUrl = URL.createObjectURL(blob);
      downloadLink.href = currentObjectUrl;
      downloadLink.download = result.fileName;
      downloadLink.classList.remove("disabled");
      setStatus(`${result.message}\n準備完了: ${result.fileName}`);
    } catch (error) {
      const message = error instanceof LzxError ? error.message : String(error);
      downloadLink.classList.add("disabled");
      setStatus(`解凍できませんでした。\n${message}`);
    }
  }

  function setFile(file) {
    revokeCurrentUrl();
    downloadLink.classList.add("disabled");
    downloadLink.removeAttribute("href");
    currentFile = file;

    if (!file) {
      selectedFile.textContent = "ファイルが選択されていません";
      setStatus("ファイルはこのブラウザ内で処理されます。サーバーへはアップロードされません。");
      return;
    }

    selectedFile.textContent = `${file.name} (${formatBytes(file.size)})`;
    setStatus("解凍準備OK");
    convertCurrentFile();
  }

  function setStatus(message) {
    status.textContent = message;
  }

  function revokeCurrentUrl() {
    if (currentObjectUrl) {
      URL.revokeObjectURL(currentObjectUrl);
      currentObjectUrl = null;
    }
  }

  function formatBytes(bytes) {
    if (bytes < 1024) {
      return `${bytes} B`;
    }

    if (bytes < 1024 * 1024) {
      return `${(bytes / 1024).toFixed(1)} KiB`;
    }

    return `${(bytes / 1024 / 1024).toFixed(2)} MiB`;
  }
})();
