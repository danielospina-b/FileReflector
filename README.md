# FileReflector

A simple web interface to select which files should be synchronized using rsync. The intended use case for this tool is to pick specific files from a Storage Server to sync to a Media Server, keeping on the remote drive only the files you want.

## Why this exists

- Couldn't find a simple tool that just did the job, so I made one.
- Take advantage of well-known linux utilities.

## How it works

- The app lists the file on the local and remote paths using ssh and local commands
- The app shows the files that are already on the destination path, and let's you select and deselect the files to sync.
- The app executes rsync and leaves only the selected files on the local path.

## Be careful

This tool aims to be secure, but it is not exhaustively designed, audited, or tested. In fact, testing is currently pretty light — “barely tested” is a better description of its state.

Please assume the following:

- It may have bugs.
- It may have security flaws.
- It may make questionable ~~life~~software development choices.
- You should absolutely inspect the source before relying on it.
You should use it locally and with caution.

This project is provided as is, with no warranty, and the authors/maintainers take no responsibility or liability for anything that happens as a result of using it.

## AI disclosure

- AI was used in this repo to generate some of the methods and documentation.
- All generated code was reviewed, not vibe-coded.
