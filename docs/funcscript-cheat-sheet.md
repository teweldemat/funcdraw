# A Collection of Concise Notes to Help Someone (Mostly AI) Quickly Understand FuncScript

## What FuncScript Is

A FuncScript program is a single expression that evaluates to a value.
A value can be an int, long, boolean, string, list, key–value collection, error, and so on.

## Common Pitfalls

1. Using if as if it were an imperative statement

Incorrect:

if x>0 then
    eval {sign:'positive',val:x}
else
    eval {sign:'negative',val:x}

Correct:

eval if x>0 then
    {sign:'positive',val:x}
else
    {sign:'negative',val:x}

Since everything is an expression, if itself must produce the value that eval will evaluate.

2. Trying to access properties of a key–value block that contains eval

Incorrect:

{
    b:{
        a:3;
        c:4;
        eval a+c;
    };
    eval b.a;   // evaluates to null because `b` itself evaluates to 7 due to the `eval` expression
}

A key–value block that contains eval resolves to the evaluated value of its eval expression, not to the key-value collection literal.
In the example above, b does not remain a block with fields a and c; it collapses to the value 7, so b.a is null.