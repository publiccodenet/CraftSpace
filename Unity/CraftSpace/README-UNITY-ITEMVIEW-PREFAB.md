# ItemViewPrefab Creation Instructions

Follow these steps to create the ItemViewPrefab:

1. In Unity, right-click in the Hierarchy window and select "Create Empty"
2. Rename the new GameObject to "ItemViewPrefab"
3. With the ItemViewPrefab GameObject selected, add these components in the Inspector:
   - Add Component > Scripts > Views > ItemView
   - Add Component > Physics > Box Collider
4. Configure the Box Collider:
   - Size: X=1.5, Y=1.5, Z=0.1
   - Is Trigger: Checked
5. Set the Layer to "Items" using the layer dropdown at the top of the Inspector
6. Configure the ItemView component settings:
   - Auto Initialize Renderers: false
   - Close Distance: 5
   - Medium Distance: 20
   - Far Distance: 100
7. Save the prefab:
   - Drag the GameObject from the Hierarchy to your Assets/Prefabs folder
   - This creates a prefab asset that can be used in the CollectionBrowserManager

The ItemViewPrefab is used as a template for each item in your collections. The renderers 
(like ArchiveTileRenderer) will be added at runtime when needed. 