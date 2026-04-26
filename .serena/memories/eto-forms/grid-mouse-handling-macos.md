# Eto.Forms Grid/Table Mouse Handling on macOS

## Platform: Eto.Forms 2.11.0 + Eto.Platform.Mac64 + MonoMac

### Key Findings

#### Event Order
- **First click on a cell (not in edit mode):** MouseDown → MouseUp → CellEditing
- **Click while cell is already in edit mode:** Only MouseUp fires. MouseDown does NOT fire. CellEditing does NOT re-fire.
- This means you cannot track drag-start (MouseDown) during cell editing on macOS.

#### Native Control Hierarchy (macOS)
- `GridView.ControlObject` → `Eto.Mac.Forms.Controls.GridViewHandler+EtoTableView` (NSTableView subclass)
- `controlObject.Window` → `EtoWindow` (NSWindow subclass)
- `window.FieldEditor(false, controlObject)` → `Eto.Mac.Forms.MacFieldEditor` (NSTextView subclass) — the active cell editor
- `controlObject.CurrentEditor` returns **null** during editing (Eto uses custom editing, not standard NSTableView editing)

#### Accessing the Field Editor via Reflection
```csharp
object controlObject = gridView.ControlObject;
var window = controlObject.GetType().GetProperty("Window")?.GetValue(controlObject);
object fieldEditor = null;
foreach (var m in window.GetType().GetMethods())
{
    if (m.Name == "FieldEditor" && m.GetParameters().Length == 2
        && m.GetParameters()[0].ParameterType == typeof(bool))
    {
        fieldEditor = m.Invoke(window, new object[] { false, controlObject });
        break;
    }
}
```

#### Reading/Setting Text in Field Editor
- Use `fieldEditor.Value` or `fieldEditor.StringValue` property to get text content.

#### Coordinate Conversion
- `fieldEditor.ConvertPointFromView(cgPoint, controlObject)` converts table-view coordinates to field-editor-local coordinates.
- The Y coordinate from Eto's MouseUp event, once converted, is typically ~40 which is outside the text line bounds (~15-20px tall).
- **Fix:** Override Y with a small constant (e.g., `2.0`) to ensure the point falls within the text line.

#### Character Index from Click Position
- Use `NSLayoutManager.CharacterIndexForPoint(CGPoint, NSTextContainer, out nfloat fractionOfDistanceThroughGlyph)`.
- The third parameter (fraction) indicates how far through the glyph the click was.
- If `fraction >= 0.5`, the cursor should be placed **after** that character (`charIndex + 1`).
- Without using the fraction, the cursor can never be placed after the last character.

#### Setting Cursor Position / Selection
- Use `fieldEditor.SelectedRange` property (NSRange struct).
- NSRange fields `Location` and `Length` are **Int64** type (not IntPtr/nint) in MonoMac.
- Must use `Activator.CreateInstance(rangeType)` to create, then set fields via `Convert.ChangeType(value, Int64)`.
- Setting Length=0 positions cursor without selection; Length>0 creates a selection.

#### CGPoint Creation
- Use `Activator.CreateInstance(cgPointType, new object[] { (double)x, (double)y })` — the constructor takes doubles.

#### Known Limitation
- Mouse drag-to-select within cells during edit mode does not work on macOS because MouseDown is not forwarded by Eto during cell editing. Only MouseUp fires, so drag start position is unavailable.
- Text selection via keyboard (Shift+arrow keys) works normally.

### Pattern: Positioning Cursor at Click in Editing Cell
1. Wire `MouseUp` handler on GridView (not MouseDown — it doesn't fire during edit).
2. Track editing state via `CellEditing`/`CellEdited` events (`_tagsEditingRow`/`_tagsEditingColumn`).
3. In MouseUp, if currently editing and click is on same row, call positioning logic via `Application.Instance.AsyncInvoke()`.
4. Get field editor via `Window.FieldEditor(false, controlObject)`.
5. Convert click coordinates via `ConvertPointFromView`.
6. Override Y to small constant (2.0) to stay within text line bounds.
7. Use `LayoutManager.CharacterIndexForPoint` with fraction for precise cursor position.
8. Set `SelectedRange` with Location=cursorPos, Length=0.
