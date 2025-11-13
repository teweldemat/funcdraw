# Example Package Building Workflow

1. Write in **FuncScript** unless JavaScript is explicitly requested.  
   (See the [language reference](../funcscript-1/docs/syntax.md))

2. Review some of the existing examples.

3. Author the example model.  
   The structure of a FuncDraw model is documented in  
   [`docs/programming-model.md`](docs/programming-model.md).

4. Test-render the model using `fd-cli`.  
   - For animated models, render key frames and inspect the generated output.

5. When debugging, you can create a dedicated **Test** expression file  
   to isolate and analyze specific problems.

---

### Debugging Tip

Wrap values with the `log` function to inspect their evaluation.  
The FuncScript runtime prints the logged value along with its tag when evaluated.

Example:
```funcscript
log(x, 'the x')
```