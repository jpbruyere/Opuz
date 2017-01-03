Opuz   [![Build Status](https://travis-ci.org/jpbruyere/Chess.svg?branch=master)](https://travis-ci.org/jpbruyere/Chess) [![Build Status Windows](https://ci.appveyor.com/api/projects/status/j387lo59vnov8jbc?svg=true)](https://ci.appveyor.com/project/jpbruyere/Opuz)
=====
**Opuz** is the contration of **Opus** and **Puzzle**. It's a small puzzle game developped in a couple of days to test my libraries, but I have to admit puzzle game programming is a recurent test of mine when testing new framework.
(So I'm maybe now, the fastest puzzle game maker in the world :smiley: )


###Building from sources
```
git clone https://github.com/jpbruyere/Opuz.git   	# Download sources
cd Opuz
git submodule update --init --recursive             # Get submodules
nuget restore										# restore nuget
xbuild  /p:Configuration=Release Opuz2015.sln       # Build
```
The resulting executable will be in **build/Release**.


###Screenshot
![Opuz](/Screenshot.png?raw=true "Opuz")


