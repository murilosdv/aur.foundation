[Home](../../README.md) · [Documentation](../README.md)

# XML documentation

Everything public is documented, and the build fails on a missing comment. Tags `<summary>`, `<remarks>` and `<example>` go on their own lines, as Visual Studio writes them; `<param>`, `<returns>` and `<typeparam>` stay on one line.

```csharp
/// <summary>
/// What it is or does, in one or two sentences.
/// </summary>
/// <param name="options">What the argument means.</param>
/// <returns>What comes back, when it is not obvious.</returns>
```

Say what the name does not.
