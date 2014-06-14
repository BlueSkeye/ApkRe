ApkRe
=====
- A set of libraries for Android Apk files reverse engineering and analysis.
- Target execution environment is Windows 7/Windows server 2008 and above with
.Net framework 4 and no other external dependency.
- For time being this is not intended to be a ready to use environment. I provide
libraries with hopefully well crafted extension points and you're expected to
build your own tools around them.

Content
=======

- ApkHandler : deals with Apk file content extraction.
- ApkRe : deals with source code regenaration from content extracted using ApkHandler
- DroidEnv : (coming soon) deals with other libraries configuration and behavioral environment
- Glue : (coming soon) a set of interfaces to be shared between project libraries and
       providing entry points for external programs using these libraries.

It doesn't work
===============

If it "doesn't work" this is either
- because my code is buggy and you should open an "Issue" with as many details as
  possible including, at least, the involved APK file(s).
- or because a feature you expect is missing which should also be handled through
  an issue.
