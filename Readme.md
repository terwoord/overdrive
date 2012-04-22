In this file:

* Introduction
* What is the current state of the project
* What is going to be committed soon
* Roadmap
* License

# Introduction

Overdrive is a library implementing deduplicated storage. It does this by providing an easy 
to use interface, which presents a easy way read and write to a store and will be able to 
reach high performance deduplication.

This basically means that it lets a developer store fixed-size blocks in a store, while transparently
deduplicating, to prevent sharing double data. The library is intended to be used by other projects,
to provide a means to open stores, and use them. 

Overdrive aims to help developers implement deduplication techniques, similar to the ones found in ZFS 
and Windows Server 2012 in their own software, while not having the full knowledge required to implement 
those techniques.

# What is the current state of the project?

Currently the library works, although the performance is less than ideal. The DeduplicatingBlockStore 
presents with a way to easily perform duplication of blocks. No caching is currently being done.

# What will be committed soon?

The following things will get published coming weeks and months:
* ODBFS: the Overdrive Filesystem. Allows splitting up one blockstore in multiple logical volumes.

# Roadmap 

Some plans are already clear, and those have been divided into longer and shorter-term plans.

## Shortterm

* Change the DeduplicatingBlockStore: With some slight changes in how the deduplication process works,
we can dramatically improve the write performance. Right now, a single write takes 5 writes (worst-case).
This can be brought back to 2 at most, by the use of delayed writing, while still staying safe for data corruption.
* Documentation
* Safe opening of stores. 

## Longer term

* Make everything thread-safe. Right now, everything is thread-safe, as long as it's used exclusively by 
one thread at a time. Making the project multithreaded, opens lots of uses, for example when combined with
Dokan (http://dokan-dev.net/en/) or as backend for some iSCSI target library.
* more performance improvements by using more sophisticated algorithms.

# License

Copyright (c) 2007-2008, The OverDrive Storage Project
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted 
provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions 
and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this list of 
conditions and the following disclaimer in the documentation and/or other materials provided 
with the distribution.

* Neither the name of The Cosmos Project nor the names of its contributors may be used to endorse 
or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY 
EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE 
COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, 
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED 
AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF 
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 