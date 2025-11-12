# How names are resolved in FuncDraw workspaces

## Resolution rules within a workspace
In FuncDraw, when a name is referenced, the value is first looked up locally within the expression. If it is not found there, the search continues among the names of sibling expressions, then among the siblings of the parent collection or module, and so on up the hierarchy.
Because of this, expressions and folders do not need to be explicitly imported within a workspace.

## Imports
The `import` function in FuncScript (`fdimport` in JavaScript contexts) is used to import packages from node_modules folders. All root items—expressions, collections, and modules—are assembled into a `KeyValueCollection` (an object in JavaScript). Imported modules do not need to specify what they export.
Name references inside imported packages follow the same workspace name resolution rules described above.


## Package Structure

- package.json
- funcdraw.json
- <expression>.fs|js
- <collection>.fs|js
    - <expression>.fs|js
    - <collection>.fs|js
        - <expression
- <module>
    - <eval>.fs|js
    - <expression>.fs|js