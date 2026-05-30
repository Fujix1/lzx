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

## Windows App

A WinForms desktop app is available under `windows/mdx_unlzx`.

- Window title: `UnLZX for MDX / PDX`
- Output executable: `mdx_unlzx.exe`
- Drop `.mdx`, `.pdx`, or folders onto the window.
- The footer status bar shows `mdx, pdx ファイルやフォルダをドロップしてください` at startup.
- The app lists file name, tag/title, and LZX-compressed status.
- Press `解凍` to decode compressed files in place.
- The original file is renamed to `.bak`, `.bak1`, and so on.
- Decoded data is first written to `.tmp`, then moved into place.

Build:

```sh
dotnet build windows/mdx_unlzx/mdx_unlzx.csproj
```

## Windows App

The WinForms version lives in `windows/mdx_unlzx`.

```sh
dotnet build windows/mdx_unlzx/mdx_unlzx.csproj -c Release
```

The output executable is:

```text
windows/mdx_unlzx/bin/Release/net6.0-windows/mdx_unlzx.exe
```

The form title is `UnLZX for MDX / PDX`. Drop MDX/PDX files or folders onto
the window to list detected files. Press `解凍` to decode LZX-compressed files
in place. The app first writes a temporary `.tmp` file, renames the original to
`.bak`, then moves the decoded file into the original path.
