// X68000 LZX042 decoder ported from Mamiya's GPL lzx042.nas.
// This file is GPL-2.0 licensed as part of this project.

(() => {
const MARKER = [0x7f, 0xff, 0xff, 0x4c];
const MDX_TITLE_TERMINATOR = [0x0d, 0x0a, 0x1a];

class LzxError extends Error {
  constructor(message) {
    super(message);
    this.name = "LzxError";
  }
}

function decodeLzx042(input, options = {}) {
  const source = asUint8Array(input);
  const streamOffset = findLzxStreamOffset(source, options.searchOffset ?? 0);
  const reader = new LzxBitReader(source, streamOffset);
  const out = [];

  for (;;) {
    if (reader.getBit() === 1) {
      out.push(reader.getByte());
      continue;
    }

    if (reader.getBit() === 0) {
      const code = (reader.getBit() << 1) | reader.getBit();
      const distanceByte = reader.getByte();
      const offset = distanceByte - 256;
      copyFromHistory(out, offset, code + 2);
      continue;
    }

    const hi = reader.getByte();
    const lo = reader.getByte();
    const packed = (hi << 8) | lo;
    const offset = (0xffff0000 | packed) >> 3;
    const code = lo & 0x07;

    if (code !== 0) {
      copyFromHistory(out, offset, code + 2);
      continue;
    }

    const extra = reader.getByte();
    if (extra === 0) {
      return new Uint8Array(out);
    }

    copyFromHistory(out, offset, extra + 1);
  }
}

function convertFileBytes(input, fileName = "input.mdx") {
  const bytes = asUint8Array(input);
  const ext = extensionOf(fileName);

  if (ext === "mdx") {
    return convertMdx(bytes, fileName);
  }

  if (ext === "pdx") {
    return convertPdx(bytes, fileName);
  }

  throw new LzxError(".MDX または .PDX ファイルを選択してください。");
}

function convertMdx(input, fileName = "input.mdx") {
  const bytes = asUint8Array(input);
  const headerEnd = findMdxHeaderEnd(bytes);
  const compressedBody = bytes.subarray(headerEnd);
  const decodedBody = decodeLzx042(compressedBody);
  const converted = concatBytes(bytes.subarray(0, headerEnd), decodedBody);

  return {
    bytes: converted,
    fileName: withSuffix(fileName, "_decoded"),
    type: "mdx",
    message:
      `MDXヘッダ ${headerEnd} バイトを保持しました。` +
      `${compressedBody.length} バイトの圧縮データを ${decodedBody.length} バイトに解凍しました。`,
  };
}

function convertPdx(input, fileName = "input.pdx") {
  const bytes = asUint8Array(input);
  const decoded = decodeLzx042(bytes);

  return {
    bytes: decoded,
    fileName: withSuffix(fileName, "_decoded"),
    type: "pdx",
    message:
      `PDX全体をLZX042ストリームとして解凍しました。` +
      `${bytes.length} バイトの圧縮データを ${decoded.length} バイトに解凍しました。`,
  };
}

function findLzxStreamOffset(input, baseOffset = 0) {
  const bytes = asUint8Array(input);
  let pos = baseOffset + 0x24;

  while (pos + 2 + MARKER.length <= bytes.length) {
    pos += 2;
    if (hasMarkerAt(bytes, pos)) {
      return pos + MARKER.length;
    }
  }

  const fallback = findMarker(bytes, Math.max(0, baseOffset));
  if (fallback >= 0) {
    return fallback + MARKER.length;
  }

  throw new LzxError("LZX042マーカー 7F FF FF 4C が見つかりません。");
}

function findMdxHeaderEnd(input) {
  const bytes = asUint8Array(input);
  const titleEnd = findSequence(bytes, MDX_TITLE_TERMINATOR, 0);
  if (titleEnd < 0) {
    throw new LzxError("MDXタイトル終端 0D 0A 1A が見つかりません。");
  }

  const pdxNameStart = titleEnd + MDX_TITLE_TERMINATOR.length;
  const pdxNameEnd = findByte(bytes, 0x00, pdxNameStart);
  if (pdxNameEnd < 0) {
    throw new LzxError("MDXのPDX名NUL終端が見つかりません。");
  }

  return pdxNameEnd + 1;
}

function concatBytes(...chunks) {
  const total = chunks.reduce((sum, chunk) => sum + chunk.length, 0);
  const result = new Uint8Array(total);
  let offset = 0;

  for (const chunk of chunks) {
    result.set(chunk, offset);
    offset += chunk.length;
  }

  return result;
}

class LzxBitReader {
  constructor(source, offset) {
    this.source = source;
    this.offset = offset;
    this.bitCount = 8;
    this.current = this.getByte();
  }

  getByte() {
    if (this.offset >= this.source.length) {
      throw new LzxError("LZX042ストリームが途中で終了しました。");
    }

    return this.source[this.offset++];
  }

  getBit() {
    this.bitCount -= 1;
    if (this.bitCount < 0) {
      this.current = this.getByte();
      this.bitCount = 7;
    }

    const bit = (this.current & 0x80) >>> 7;
    this.current = (this.current << 1) & 0xff;
    return bit;
  }
}

function copyFromHistory(out, offset, length) {
  let source = out.length + offset;
  if (offset >= 0 || source < 0) {
    throw new LzxError(`不正な後方参照オフセットです: ${offset}`);
  }

  for (let i = 0; i < length; i += 1) {
    out.push(out[source++]);
  }
}

function asUint8Array(input) {
  if (input instanceof Uint8Array) {
    return input;
  }

  if (input instanceof ArrayBuffer) {
    return new Uint8Array(input);
  }

  if (ArrayBuffer.isView(input)) {
    return new Uint8Array(input.buffer, input.byteOffset, input.byteLength);
  }

  throw new TypeError("ArrayBuffer または Uint8Array が必要です。");
}

function hasMarkerAt(bytes, offset) {
  return MARKER.every((value, index) => bytes[offset + index] === value);
}

function findMarker(bytes, offset) {
  for (let i = offset; i + MARKER.length <= bytes.length; i += 1) {
    if (hasMarkerAt(bytes, i)) {
      return i;
    }
  }
  return -1;
}

function findSequence(bytes, sequence, offset) {
  for (let i = offset; i + sequence.length <= bytes.length; i += 1) {
    let matched = true;
    for (let j = 0; j < sequence.length; j += 1) {
      if (bytes[i + j] !== sequence[j]) {
        matched = false;
        break;
      }
    }
    if (matched) {
      return i;
    }
  }
  return -1;
}

function findByte(bytes, value, offset) {
  for (let i = offset; i < bytes.length; i += 1) {
    if (bytes[i] === value) {
      return i;
    }
  }
  return -1;
}

function extensionOf(fileName) {
  const dot = fileName.lastIndexOf(".");
  return dot >= 0 ? fileName.slice(dot + 1).toLowerCase() : "";
}

function withSuffix(fileName, suffix) {
  const slash = Math.max(fileName.lastIndexOf("/"), fileName.lastIndexOf("\\"));
  const dot = fileName.lastIndexOf(".");
  const hasExtension = dot > slash;

  if (!hasExtension) {
    return `${fileName}${suffix}`;
  }

  return `${fileName.slice(0, dot)}${suffix}${fileName.slice(dot)}`;
}

globalThis.X68000Lzx = {
  LzxError,
  concatBytes,
  convertFileBytes,
  convertMdx,
  convertPdx,
  decodeLzx042,
  findLzxStreamOffset,
  findMdxHeaderEnd,
};
})();
