# Getting Started

## 1. Launch the hosted FuncDraw Player
- Open **https://funcdraw.app** in your browser. The hosted build always runs the latest renderer, reference docs, and sample workspaces.

## 2. Load a sample workspace
- Click the **Example** button (the blue book icon) in the toolbar. Pick a collection such as **Ghost Bicyle** and the player will load every tab for you—no manual file browsing required.

## 3. Iterate on expressions
- Edit any tab right in the browser. As soon as the `graphics` tab returns a new value, the canvas re-renders.
- Need a reminder on primitive shapes? Open the **Reference** dialog (question‑mark icon) for inline docs and a link back to the full FuncDraw documentation.

## 4. Download your workspace
- Use the **Download Workspace** button to grab a `.fdmodel` archive. That archive mirrors the folders you see in the player, so you can re-import it later or feed it to the CLI (`fd-cli --root ./workspace --expression graphics/main`).

## 5. Export SVG snapshots
- Select **Export SVG** to capture a deterministic vector snapshot at any time value. SVG exports land in your downloads folder and can be opened in Illustrator, Figma, or version control.

## 6. Reuse helpers by importing into a collection
- Whenever you download a workspace—or receive one from a teammate—you can bring specific helpers into your own project. Choose **Import Library**, pick the `.fdmodel` file, and select which folders or tabs to merge. The player creates a custom collection so those helpers stay organized alongside your work.

With https://funcdraw.app plus the CLI you can iterate, download, share, and re-import FuncDraw scenes without touching local environment setup.
