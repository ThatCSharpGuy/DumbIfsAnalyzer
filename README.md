# DumbIfsAnalyzer  
  
## What is this?
This is the source code for a [Roslyn](https://github.com/dotnet/roslyn) based analysis tool to detect what I call *dumb ifs*, i. e. 

```  
if(true)  
{  
    // more crappy code
}  
```  

Yes, that is the real life, isnt just fanta-sea.

## Why?
My main goal was only to mess around with the wonderful APIs that Roslyn provides, but now I think I can achieve the world peace by enforcing beautiful code.  

## How does this work?
Basically this takes all the `if` senteces and process their condition, tries to find wether it is a constant value (`true`, `false`, `!false && true || false`...) and **throws an error on build** if that is the case.  
  
Special thanks to [this blog post](https://msdn.microsoft.com/en-us/magazine/dn879356.aspx) in which is entirely based this analyzer.

See more here... well not yet.
