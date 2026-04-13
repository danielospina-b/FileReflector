# FileReflector

A simple web interface to select which files should be synchronized using rsync. The intended use case for this tool is to pick specific files from a Storage Server to sync to a Media Server, keeping on the remote drive only the files you want.

## Why this exists

- Couldn't find a simple tool that just did the job, so I made one.
- Take advantage of well-known linux utilities.

## How it works

- The app lists the file on the local and remote paths.
- The app shows the files that are already on the destination path, and let's you select and deselect the files to sync.
- The app executes rsync and leaves only the selected files on the local path.

## AI disclosure

- AI was used in this repo to generate some of the methods and documentation.
- All generated code was reviewed, not vibe-coded.
