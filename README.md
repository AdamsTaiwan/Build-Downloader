# Build-Downloader
Microsoft Build Downloader Tool

Build-Downloader is a tool to help download the resource text and links from Microsoft's Build 2019 event and generate text that can then be copied to OneNote.

## Features

- Download session data
- Load from local store
- Filter sessions
- Download slides
- Download videos
- Generate text for OneNote
- Can now download resources from Ignite 2019
- Build 2019 is no longer available
- Build 2020 is now available
- Ignite 2020 is now available
- Now Multi-Targets: .Net 4.7.2 and .Net 5.0

## Usage

- Select a path for the downloaded session media.
- A DefaultPath can be set in the App.config file.
- A FeelList.xml file contains urls and output paths to conference resources.
- Click Open to open the path in Explorer.
- Click Load to open a previous downloaded session list.
- Click Download to download sessions.
- Use the filter options to limit the list of sessions.
- Select sessions then Get Slides, Videos or to make a web view list.
- Once the Web view list is made. With the mouse click on the web view and press Ctrl + A, Ctrl + C. Then open OneNote and paste.
- The markup that is generated in the Web view can be changed using the Template tab.  The **template** element will be repeated for each selected session. 
