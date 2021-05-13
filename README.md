# BookShuffler

*BookShuffler* is an open-source visual document layout tool similar to Scrivener's corkboard feature.  It was made to work with markdown to support authors who use a flat-text workflow and was created due to the dearth of such tools.

*BookShuffler* is in an early proof-of-concept stage and was put together quickly to aid in the reorganization of one particular 400+ page book for an author facing a deadline.  Features were added as needed to support that effort, but given that *BookShuffler* turned out to be a useful tool I would like to develop it further, time permitting.

## Features

Currently *BookShuffler* can ingest annotated markdown, which it will break into "sections" and "cards" and store as flat files in a project folder.  A writing project's structure takes the form of a tree, where "cards" are leaves and "sections" are nodes with children.

Every "section" becomes a corkboard, where all of the child nodes can be dragged around to change the order of the content.  "Cards" can be annotated with user-defined categories which can be assigned colors, making it easy to visually see the structure of the work.

Any node in the tree, "section" or "card", can be detached from the project tree and placed into a bin of detached entities.  From there it can be re-attached to any "section" in the tree, allowing large portions of the document to be moved around and restructured with ease.

Finally, any "section" (including the root of the tree which effectively contains the entire document) can be re-assembled into a single document with the new ordering of children.  Thus the entire writing project can be imported, re-arranged, and re-assembled with ease.

## Language and Framework

*BookShuffler* is written in modern C# (.NET 5) and uses Avalonia as a cross platform GUI framework.  It is cross platform and has been used on Linux and MacOS, and will likely work on Windows as well.

