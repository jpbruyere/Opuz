Opuz
=====
OpenGL puzzle game.

Small game developped in a couple of days to test my libraries.

Building
========

On debian, check several cil bindings:
```
sudo apt-get install libmono-cairo4.0-cil libglib3.0-cil librsvg2-2.18-cil
```


```
git clone --recursive https://github.com/jpbruyere/Opuz.git   	# Download sources from git
cd Opuz/lib/opentk                                              
nuget restor OpenTK.sln                                         # Update nuget packages
xbuild  /p:Configuration=Release OpenTK.sln                     # build opentk
cd ../..
xbuild  /p:Configuration=Release Opuz2015.sln                   # build Opuz
```


note:
-----

I use special fonts that maybe not available on your system,
I have to improve font handling in GOLib to use fallback ones if not found.

Fonts are:
- Rothenburg Decorative
- Orange Juice

Screenshot
==========

![Opuz](/Screenshot.png?raw=true "Opuz")


