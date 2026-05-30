import assert from "node:assert/strict";
import "../src/lzx042.js";

const { decodeLzx042, findMdxHeaderEnd, convertMdx } = globalThis.X68000Lzx;

const marker = [0x7f, 0xff, 0xff, 0x4c];

function makeContainer(stream) {
  return new Uint8Array([...new Array(0x26).fill(0), ...marker, ...stream]);
}

function ascii(text) {
  return new TextEncoder().encode(text);
}

function bytesToText(bytes) {
  return new TextDecoder().decode(bytes);
}

{
  const compressed = makeContainer([
    0xf4, // 1111, then long-copy/end command prefix 01.
    0x41,
    0x42,
    0x43,
    0x44,
    0x00,
    0x00,
    0x00,
  ]);

  assert.equal(bytesToText(decodeLzx042(compressed)), "ABCD");
}

{
  const compressed = makeContainer([
    0xe0, // literals A/B/C, then short copy with length code 0.
    0x41,
    0x42,
    0x43,
    0xfd, // offset -3.
    0x80,
    0x00,
    0x00,
    0x00,
  ]);

  assert.equal(bytesToText(decodeLzx042(compressed)), "ABCAB");
}

{
  const compressed = makeContainer([
    0xea, // literals A/B/C, long copy, then end command prefix.
    0x41,
    0x42,
    0x43,
    0xff,
    0xe9, // offset -3, length code 1 => length 3.
    0x00,
    0x00,
    0x00,
  ]);

  assert.equal(bytesToText(decodeLzx042(compressed)), "ABCABC");
}

{
  const compressed = makeContainer([
    0xc1, // literals A/B, short copy from -1, then end command prefix.
    0x41,
    0x42,
    0xff, // offset -1, length 2. This overlaps like REP MOVSB.
    0x80,
    0x00,
    0x00,
    0x00,
  ]);

  assert.equal(bytesToText(decodeLzx042(compressed)), "ABBB");
}

{
  const header = new Uint8Array([...ascii("TITLE"), 0x0d, 0x0a, 0x1a, ...ascii("mag"), 0x00]);
  const body = makeContainer([
    0xd0,
    0x58,
    0x59,
    0x00,
    0x00,
    0x00,
  ]);
  const mdx = new Uint8Array([...header, ...body]);
  const converted = convertMdx(mdx, "sample.MDX");

  assert.equal(findMdxHeaderEnd(mdx), header.length);
  assert.equal(bytesToText(converted.bytes.slice(0, header.length)), "TITLE\r\n\u001amag\0");
  assert.equal(bytesToText(converted.bytes.slice(header.length)), "XY");
  assert.equal(converted.fileName, "sample_decoded.MDX");
}

console.log("decoder tests passed");
