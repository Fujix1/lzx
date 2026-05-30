# X68000 MDX/PDX LZX Decoder

Browser-only converter for X68000 MDX/PDX files compressed with the LZX042
format used by some MDX archives. This is a separate GPL project and is not
part of NanoDrive8.

## Policy

- The converter runs entirely in the browser.
- Files are read through the local File API.
- No file is uploaded to a server.
- LZX-compressed MDX/PDX remains unsupported by NanoDrive8 itself.

## License And Origin

This project is licensed under GPL-2.0 because the decoder is derived from:

- `lzx042.nas`
- KUMAamp project
- Copyright (C) Mamiya 2000.
- License: GPL
- Reference source: https://github.com/FIX94/in_mdx/tree/master/LZX042

The JavaScript decoder in `src/lzx042.js` is a clean port of the control flow
and bitstream rules shown in `lzx042.nas`.

## Usage

Open `index.html` in a modern browser, choose an `.MDX` or `.PDX` file, and
download the converted output.

MDX support preserves the clear MDX title and PDX-name header, then replaces the
compressed music body after the PDX-name NUL terminator with the decompressed
body.

PDX support is currently conservative: the tool attempts whole-file LZX042
decoding when the marker is present. More sample files are needed to confirm
whether compressed PDX archives are whole-file or partial-body compressed.

## Development

Run the decoder self-test with Node.js:

```sh
node test/decoder.test.mjs
```
