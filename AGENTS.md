# FuncDraw Vector Graphics Authoring System

FuncDraw is a vector graphics authoring system built around a strictly component-based model for graphics, motion, and transformation. It is powered by a functional programming language called **FuncScript** and brings an open‑source, node‑style component authoring culture to vector graphics.

In FuncDraw, artwork is authored as code. Visual structures, behaviors, and animations are composed from small, reusable functional components and organized in a filesystem‑driven hierarchy.

An **art package** is distributed as an npm package and contains an *art root folder* with FuncScript expressions organized into folders. Folder and file names are referenced directly in FuncScript expressions and therefore form part of the public API of the art package.

## Anatomy of a FuncDraw Package

**Package**  
An npm package that contains an art root folder with FuncScript expressions.

**Expression**  
A `.fs` file within the art folder structure. An expression evaluates to a value, typically a drawable, transformation, animation, or higher‑order component.

**Collection**  
A folder in the art folder structure that contains expressions and/or subfolders and does *not* contain an `eval.fs` file.

A collection evaluates to a key–value object where:
- each key is the file or folder name, and
- each value is the evaluated result of the corresponding expression or sub‑collection.

Collections expose their internal structure directly through this mapping.

**Module**  
A folder that contains an `eval.fs` file. The presence of `eval.fs` turns the folder into a module.

A module evaluates exclusively to the result of `eval.fs`. Its internal files and folders are not directly accessible, and the module is referenced only by its folder name.

## The `package` Function

The `package("<npm-package-name>")` function loads a FuncDraw art package and evaluates its art root folder.

- If the art root folder contains an `eval.fs` file, the result of that file is returned.
- Otherwise, the art root folder is evaluated as a collection and returns a key–value object representing its contents.