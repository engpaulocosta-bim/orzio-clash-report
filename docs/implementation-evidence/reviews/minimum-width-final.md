**Verdict: no critical or high issues introduced. Static review only; nothing was built or run.**

**Locale keys and contracts:** unchanged. Only the value of `home.tile.compare.action` changed (`UiStrings.Home.cs:41`). The key is the same, both PT and EN translations are present, and there are no placeholders to mismatch. The only references are the `{loc:Loc home.tile.compare.action}` binding in `HomeView.axaml:115` and the definition itself. No test or other code matches the old strings.

**Test change:** `DesignViewportTests.cs` adds a 1024×680 case and a per-action clipping check. It compiles on reading: `Avalonia.Point?` is handled after `Assert.NotNull`, and `using Avalonia;` is added. It is a sound check, with three limits:
- It checks only the horizontal edge (`X + Width` against the card's right padding, with 1px tolerance). It does not check vertical clipping or height.
- It depends on the `link` and `card` style class names, which I did not verify against the XAML. If either is wrong, the loop either asserts nothing or throws.
- It is unexecuted. The new InlineData case and the clipping assertion need a `dotnet test` run before anyone claims they pass.

**Scope note:** `design-final.md` is the earlier verdict on the `InvalidDataException` handling. It does not cover the minimum-width or label changes. The "no critical/high" conclusion therefore comes from the diff itself, not from that document.

**Minor:** the shorter EN/PT action labels are still user-visible copy. Check them in the rendered 1024px layout, since the test does not confirm readability.
