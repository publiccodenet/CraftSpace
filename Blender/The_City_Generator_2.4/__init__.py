import bpy
import os

bl_info = {
    "name": "The City Generator",
    "blender": (4, 3, 0),
    "category": "Object",
    "description": "An add-on to generate a City",
    "author": "Andreas Dürr",
    "version": (1, 0, 0),
    "location": "View3D > Sidebar > City Generator",
}

# Constants
ADDON_DIR = os.path.dirname(os.path.abspath(__file__))
BLEND_FILE = "City_Generator2.0.blend"

# Utility Functions

def append_data(directory, filename):
    """Appends data from a blend file."""
    path = os.path.join(ADDON_DIR, BLEND_FILE, directory)
    bpy.ops.wm.append(directory=path, filename=filename, autoselect=True)

def get_or_create_attribute(mesh, name, attr_type, domain):
    """Gets or creates a custom attribute on the given mesh."""
    if name not in mesh.attributes:
        mesh.attributes.new(name=name, type=attr_type, domain=domain)
    return mesh.attributes[name]





def find_layer_collection(layer_collection, collection_name):
    """Recursively find a layer collection by name."""
    if layer_collection.name == collection_name:
        return layer_collection
    for child in layer_collection.children:
        result = find_layer_collection(child, collection_name)
        if result:
            return result
    return None




# Operators

class CG_OT_Import_Node_Group(bpy.types.Operator):
    bl_idname = 'cg.import_node_group'
    bl_label = 'Import Node Group'
    bl_description = "Import City Generator Node Group"
    bl_options = {'PRESET', 'UNDO'}

    def execute(self, context):
        node_group_name = 'City_Generator_2.0'
        city_object_name = 'City_Generator_2.0_Object'

        # Import the City object
        if city_object_name not in bpy.data.objects:
            append_data("Object", city_object_name)
            self.report({'INFO'}, f"'{city_object_name}' object imported successfully.")
        else:
            self.report({'INFO'}, f"'{city_object_name}' object already exists.")

        # Import node group
        if node_group_name not in bpy.data.node_groups:
            append_data("NodeTree", node_group_name)
            self.report({'INFO'}, f"'{node_group_name}' node group imported successfully.")
        else:
            self.report({'INFO'}, f"'{node_group_name}' node group already exists.")

        # Manage the collection visibility
        assets_collection_name = 'City_Gen_2.0_Assets'
        layer_collection = find_layer_collection(context.view_layer.layer_collection, assets_collection_name)

        if layer_collection:
            layer_collection.exclude = True  # Exclude from the viewport
            context.view_layer.update()  # Refresh the view layer to apply changes
        else:
            self.report({'WARNING'}, f"Layer collection '{assets_collection_name}' not found.")

        return {'FINISHED'}




class CG_OT_Apply_Node_Group(bpy.types.Operator):
    bl_idname = 'cg.apply_node_group'
    bl_label = 'Apply Node Group'
    bl_description = "Apply City Generator Node Group to Active Object"
    bl_options = {'PRESET', 'UNDO'}

    @classmethod
    def poll(cls, context):
        return context.mode == 'OBJECT' and context.active_object and context.active_object.type == 'MESH'

    def execute(self, context):
        node_group_name = 'City_Generator_2.0'
        obj = context.active_object

        # Import node group if missing
        if node_group_name not in bpy.data.node_groups:
            append_data("NodeTree", node_group_name)

        # Apply node group as a modifier
        if node_group_name not in obj.modifiers:
            mod = obj.modifiers.new(type='NODES', name=node_group_name)
            mod.node_group = bpy.data.node_groups.get(node_group_name)
        else:
            self.report({'WARNING'}, f"'{node_group_name}' modifier already exists on the active object!")
            
            
        # Manage the collection visibility
        assets_collection_name = 'City_Gen_2.0_Assets'
        layer_collection = find_layer_collection(context.view_layer.layer_collection, assets_collection_name)

        if layer_collection:
            layer_collection.exclude = True  # Exclude from the viewport
            context.view_layer.update()  # Refresh the view layer to apply changes
            self.report({'INFO'}, f"Collection '{assets_collection_name}' visibility updated.")
        else:
            self.report({'WARNING'}, f"Layer collection '{assets_collection_name}' not found.")


        return {'FINISHED'}

class CG_PT_Main_Panel(bpy.types.Panel):
    bl_label = "Import City Generator"
    bl_idname = "CG_PT_main_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'

    def draw(self, context):
        layout = self.layout
        box = layout.box()

        row = box.row()
        row.scale_y = 2.0
        row.operator("cg.import_node_group", text="Import City Generator", icon='IMPORT')

        row = box.row()
        row.scale_y = 2.0
        row.operator("cg.apply_node_group", text="Apply Node Group", icon='NODETREE')
        
        
        
        

# City Generator Settings
class CG_Setting_Panel(bpy.types.Panel):
    bl_label = "City Generator Settings"
    bl_idname = "CG_Setting_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_options = {'DEFAULT_CLOSED'}

    def draw(self, context):
        layout = self.layout
        
        



class CG_OT_Duplicate_Object(bpy.types.Operator):
    bl_idname = "cg.duplicate_object"
    bl_label = "Duplicate Object with Modifier"
    bl_description = "Duplicate the object with the same Geometry Nodes modifier and rename it to 'CityGen Buildings'"
    bl_options = {'REGISTER', 'UNDO'}
    
    def execute(self, context):
        obj = context.object
        
        # Check if the object and its modifiers exist
        if obj and obj.modifiers:
            geo_mod = obj.modifiers.get("City_Generator_2.0")
            if geo_mod:
                obj.modifiers["City_Generator_2.0"]["Socket_163"] = True

                # Duplicate the object
                bpy.ops.object.duplicate()

                # Get the duplicated object
                duplicate = context.object
                duplicate.name = "CityGen Buildings"  # Rename the duplicate

                # Apply the modifier
                bpy.ops.object.mode_set(mode='OBJECT')
                bpy.ops.object.modifier_apply(modifier=geo_mod.name)

                # Add the modifier back to the duplicate
                new_mod = duplicate.modifiers.new(name="City_Generator_2.0", type="NODES")
                new_mod.node_group = geo_mod.node_group  # Copy the node group

                # Copy all other settings from the original modifier
                for prop in geo_mod.keys():
                    if prop not in {"rna_type", "name", "type", "node_group"}:  # Skip default keys
                        new_mod[prop] = geo_mod[prop]

                
                
                geo_mod["Socket_163"] = False  # Original object
                geo_mod["Socket_142"] = False  # Original object
                
                new_mod["Socket_163"] = False  # Duplicate object
                new_mod["Socket_145"] = True  # Set the new socket for the duplicate
                
                
                # Programmatically disable and re-enable the modifier for the duplicate object
                geo_mod.show_viewport = False
                bpy.context.view_layer.update()
                geo_mod.show_viewport = True
                bpy.context.view_layer.update()


                self.report({'INFO'}, "Object duplicated and modifier re-added successfully!")
                return {'FINISHED'}
            else:
                self.report({'WARNING'}, "The object does not have a 'City_Generator_2.0' modifier.")
                return {'CANCELLED'}
        else:
            self.report({'WARNING'}, "No active object or modifiers found.")
            return {'CANCELLED'}



class MESH_OT_SetLowPolyAttribute(bpy.types.Operator):
    bl_idname = "mesh.set_low_poly_attribute"
    bl_label = "Set Low Poly"
    value: bpy.props.IntProperty()

    def execute(self, context):
        if context.object.mode == 'EDIT' and context.object.type == 'MESH':
            bpy.ops.object.mode_set(mode='OBJECT')
            mesh = context.object.data
            for poly in mesh.polygons:
                if poly.select:
                    if "low poly" not in mesh.attributes:
                        mesh.attributes.new(name="low poly", type='INT', domain='FACE')
                    mesh.attributes["low poly"].data[poly.index].value = self.value
            bpy.ops.object.mode_set(mode='EDIT')
        return {'FINISHED'}








class CG_General_Setting_Panel(bpy.types.Panel):
    bl_label = "General Settings"
    bl_idname = "CG_General_Setting_Panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_parent_id = 'CG_Setting_panel'
    bl_options = {'DEFAULT_CLOSED'}

    def draw(self, context):
        layout = self.layout
        
        obj = context.object
        
        if obj and obj.modifiers:
            mod = obj.modifiers.get("City_Generator_2.0")
            if mod:
                box = layout.box()
                box.prop(mod, '["Socket_8"]', text="Layout Edit Mode")
                box.label(text="Activated Elements")
                box.prop(mod, '["Socket_142"]', text="Buildings")
                box.prop(mod, '["Socket_143"]', text="Streets")
                box.prop(mod, '["Socket_144"]', text="Traffic")
                row = box.row()
                row.scale_y = 2.0
                row.operator("cg.duplicate_object", text="Seperate Buildings for more controll", icon='DUPLICATE')
                row = box.row()
                row.scale_y = 1.5
                row.operator("mesh.set_low_poly_attribute", text="Assign Low Poly", icon='FACESEL').value = 1
                row.operator("mesh.set_low_poly_attribute", text="Remove Low Poly", icon='FACESEL').value = 0
                row = box.row()
                box.prop(mod, '["Socket_165"]', text="Realize Instances")
                box.prop(mod, '["Socket_187"]', text="Real Mesh")
                box.prop(mod, '["Socket_188"]', text="Instances")


        
        else:
            layout.label(text="No active object selected.", icon='ERROR')
            
            
            
            
            
            

            
            











































            
            

class CG_Street_Setting_Panel(bpy.types.Panel):
    bl_label = "Street Settings"
    bl_idname = "CG_Street_Setting_Panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_parent_id = 'CG_Setting_panel'
    bl_options = {'DEFAULT_CLOSED'}
    
    def draw(self, context):
        layout = self.layout
        
        obj = context.object
        
        if obj and obj.modifiers:
            mod = obj.modifiers.get("City_Generator_2.0")
            if mod:
                box = layout.box()
                box.prop(mod, '["Socket_9"]', text="Street Width")
                box.prop(mod, '["Socket_12"]', text="Lane Amount")
                box.prop(mod, '["Socket_16"]', text="Side Walk Scale")
                box.prop(mod, '["Socket_20"]', text="Parking Lanes Probability")
                box.prop(mod, '["Socket_21"]', text="Seed")
        else:
            layout.label(text="No active object selected.", icon='ERROR')
            
            
            
            
            
class MESH_OT_AddParkAttribute(bpy.types.Operator):
    bl_idname = "mesh.add_park_attribute"
    bl_label = "Add Park"
    value: bpy.props.IntProperty()

    def execute(self, context):
        if context.object.mode == 'EDIT' and context.object.type == 'MESH':
            bpy.ops.object.mode_set(mode='OBJECT')
            mesh = context.object.data
            for poly in mesh.polygons:
                if poly.select:
                    if "assign Park" not in mesh.attributes:
                        mesh.attributes.new(name="assign Park", type='INT', domain='FACE')
                    mesh.attributes["assign Park"].data[poly.index].value = self.value
            bpy.ops.object.mode_set(mode='EDIT')
        return {'FINISHED'}


class CG_Park_Setting_Panel(bpy.types.Panel):
    bl_label = "Park Settings"
    bl_idname = "CG_Park_Setting_Panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_parent_id = 'CG_Street_Setting_Panel'
    bl_options = {'DEFAULT_CLOSED'}
    
    def draw(self, context):
        layout = self.layout
        
        obj = context.object
        
        if obj and obj.modifiers:
            mod = obj.modifiers.get("City_Generator_2.0")
            if mod:
                box = layout.box()
                box.scale_y = 1.5
                row = box.row()  # Properly define the row
                row.operator("mesh.add_park_attribute", text="Add Park", icon='FACESEL').value = 1
                row.operator("mesh.add_park_attribute", text="Remove Park", icon='FACESEL').value = 0
                box = layout.box()
                box.prop(mod, '["Socket_154"]', text="Path Subdivision")
                box.prop(mod, '["Socket_155"]', text="Path Seed")
                box.prop(mod, '["Socket_156"]', text="Path Iterations")
                box.prop(mod, '["Socket_157"]', text="Path Radius")
                box.prop(mod, '["Socket_158"]', text="Tree Distance Min")
                box.prop(mod, '["Socket_159"]', text="Tree Density Factor")
                box.prop(mod, '["Socket_160"]', text="Tree Seed")
                box.prop(mod, '["Socket_161"]', text="Min Scale")
                box.prop(mod, '["Socket_162"]', text="Max Scale")
        
        
        
        
        
        
        else:
            layout.label(text="No active object selected.", icon='ERROR')
           
            
            
            
            
            
class MESH_OT_Add_Intersection_Grid(bpy.types.Operator):
    bl_idname = "mesh.set_intersection_grid"
    bl_label = "Set Intersection Grid"
    value: bpy.props.IntProperty()

    def execute(self, context):
        obj = context.object
        if obj.mode == 'EDIT' and obj.type == 'MESH':
            bpy.ops.object.mode_set(mode='OBJECT')  # Switch to Object mode
            mesh = obj.data
            for vert in mesh.vertices:  # Iterate over vertices
                if vert.select:  # Check if the vertex is selected
                    if "add intersection grid" not in mesh.attributes:
                        mesh.attributes.new(name="add intersection grid", type='INT', domain='POINT')
                    mesh.attributes["add intersection grid"].data[vert.index].value = self.value
            bpy.ops.object.mode_set(mode='EDIT')  # Return to Edit mode
        return {'FINISHED'}
            
            
            
class MESH_OT_Delete_CrossWalk(bpy.types.Operator):
    bl_idname = "mesh.delete_crosswalk"
    bl_label = "delete crosswalk"
    value: bpy.props.IntProperty()

    def execute(self, context):
        obj = context.object
        if obj.mode == 'EDIT' and obj.type == 'MESH':
            bpy.ops.object.mode_set(mode='OBJECT')  # Switch to Object mode
            mesh = obj.data
            for vert in mesh.vertices:  # Iterate over vertices
                if vert.select:  # Check if the vertex is selected
                    if "delete cross walk" not in mesh.attributes:
                        mesh.attributes.new(name="delete cross walk", type='INT', domain='POINT')
                    mesh.attributes["delete cross walk"].data[vert.index].value = self.value
            bpy.ops.object.mode_set(mode='EDIT')  # Return to Edit mode
        return {'FINISHED'}
            
            
    
    
class MESH_OT_Add_Bus_Lane(bpy.types.Operator):
    bl_idname = "mesh.add_bus_lane"
    bl_label = "Add Bus Lane"
    value: bpy.props.IntProperty()

    def execute(self, context):
        obj = context.object
        if obj.mode == 'EDIT' and obj.type == 'MESH':
            bpy.ops.object.mode_set(mode='OBJECT')  # Switch to Object mode
            mesh = obj.data
            for edge in mesh.edges:  # Iterate through edges instead of polygons
                if edge.select:  # Check if the edge is selected
                    if "add bus lane" not in mesh.attributes:
                        mesh.attributes.new(name="add bus lane", type='INT', domain='EDGE')
                    mesh.attributes["add bus lane"].data[edge.index].value = self.value
            bpy.ops.object.mode_set(mode='EDIT')  # Return to Edit mode
        return {'FINISHED'}
 
    
    

    
class MESH_OT_delete_Trees_Edge(bpy.types.Operator):
    bl_idname = "mesh.delete_trees_from_edge"
    bl_label = "Delete Trees from Edge"
    value: bpy.props.IntProperty()

    def execute(self, context):
        obj = context.object
        if obj.mode == 'EDIT' and obj.type == 'MESH':
            bpy.ops.object.mode_set(mode='OBJECT')  # Switch to Object mode
            mesh = obj.data
            for edge in mesh.edges:  # Iterate through edges instead of polygons
                if edge.select:  # Check if the edge is selected
                    if "delete Trees from Edge" not in mesh.attributes:
                        mesh.attributes.new(name="delete Trees from Edge", type='INT', domain='EDGE')
                    mesh.attributes["delete Trees from Edge"].data[edge.index].value = self.value
            bpy.ops.object.mode_set(mode='EDIT')  # Return to Edit mode
        return {'FINISHED'}    
    
    
    
              
            
            
            
class CG_Street_Adv_Setting_Panel(bpy.types.Panel):
    bl_label = "Advanced Settings"
    bl_idname = "CG_Street_Adv_Setting_Panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_parent_id = 'CG_Street_Setting_Panel'
    bl_options = {'DEFAULT_CLOSED'}
    
    def draw(self, context):
        layout = self.layout
        
        # Access the active object
        obj = context.object
        
        if obj and obj.modifiers:
            mod = obj.modifiers.get("City_Generator_2.0")
            
            if mod:
                # Create a new box for better visual grouping
                box = layout.box()

        # Standard properties (Sockets 22-25)
        box.prop(mod, '["Socket_22"]', text="Corner Radius")
        box.prop(mod, '["Socket_23"]', text="Sidewalk Height")
        box.prop(mod, '["Socket_24"]', text="Curb Width")
        box.prop(mod, '["Socket_25"]', text="Curb Height")

        # Colection Panel
        row = box.row()
        row.prop_search(mod, '["Socket_26"]', bpy.data, "collections", text="Decal Collection", icon='OUTLINER_COLLECTION')
        box.prop(mod, '["Socket_27"]', text="Decal Distance Min")
        
        box.label(text="Tree Settings")
        row = box.row()
        row.prop_search(mod, '["Socket_166"]', bpy.data, "collections", text="Tree Collection", icon='OUTLINER_COLLECTION')
        box.prop(mod, '["Socket_169"]', text="Tree Corner Distance")
        box.prop(mod, '["Socket_170"]', text="Tree Position")
        box.prop(mod, '["Socket_185"]', text="Tree Random Face Probability")
        box.prop(mod, '["Socket_186"]', text="Seed")
        box.prop(mod, '["Socket_167"]', text="Tree Random Edge Probability")
        box.prop(mod, '["Socket_168"]', text="Seed")
        row = box.row()
        row.scale_y = 1.5
        row.operator("mesh.delete_trees_from_edge", text="Delete Trees from Edge", icon='EDGESEL').value = 1
        row.operator("mesh.delete_trees_from_edge", text="Add Trees to Edge", icon='EDGESEL').value = 0
        box.prop(mod, '["Socket_171"]', text="Tree Distance")
        box.prop(mod, '["Socket_172"]', text="Delete Tree Probability")
        box.prop(mod, '["Socket_173"]', text="Seed")
        box.prop(mod, '["Socket_182"]', text="Tree Min Scale")
        box.prop(mod, '["Socket_183"]', text="Tree Max Scale")
        box.prop(mod, '["Socket_184"]', text="Seed")
        
        
        box.label(text="Crosswalk Settings")
        row = box.row()
        row.scale_y = 1.5
        row.operator("mesh.delete_crosswalk", text="Remove Crosswalk", icon='VERTEXSEL').value = 1
        row.operator("mesh.delete_crosswalk", text="Add Crosswalk", icon='VERTEXSEL').value = 0
        
        # Material Panel
        row = box.row()
        row.label(text="Crosswalk Material: ", icon='MATERIAL')
        row.prop_search(mod, '["Socket_29"]', bpy.data, "materials", text="")
        
        box.prop(mod, '["Socket_30"]', text="Crosswalk Length")
        box.prop(mod, '["Socket_31"]', text="Crosswalk Width")
        box.prop(mod, '["Socket_32"]', text="Crosswalk Width Scale")
        
        box.label(text="Lane Settings")
        row = box.row()
        row.scale_y = 1.5
        row.operator("mesh.add_bus_lane", text="Add Bus Lane", icon='EDGESEL').value = 1
        row.operator("mesh.add_bus_lane", text="Remove Bus Lane", icon='EDGESEL').value = 0
        box.prop(mod, '["Socket_35"]', text="Lane Distance")
        box.prop(mod, '["Socket_36"]', text="Marking Radius")
        box.prop(mod, '["Socket_37"]', text="Dashed Lines Probability")
        box.prop(mod, '["Socket_38"]', text="Dashed Lines Seed")
        box.prop(mod, '["Socket_39"]', text="Dashed Lines Length")
        box.prop(mod, '["Socket_40"]', text="Stop Line Width")
        
        
            
        # Colection Panel
        row = box.row()
        row.prop_search(mod, '["Socket_41"]', bpy.data, "collections", text="", icon='OUTLINER_COLLECTION')
        row = box.row()
        
        
        # Material Panel
        row.label(text="Select Material: ", icon='MATERIAL')
        row.prop_search(mod, '["Socket_42"]', bpy.data, "materials", text="")
        row = box.row()
 
        


        box.label(text="Side Lanes")                
        box.prop(mod, '["Socket_44"]', text="Side Lanes Min")
        box.prop(mod, '["Socket_45"]', text="Side Lanes Max")
        box.prop(mod, '["Socket_46"]', text="Seed")
        box.prop(mod, '["Socket_47"]', text="Diagonal Lines Distance")
        box.label(text="Bike Lanes")
        box.prop(mod, '["Socket_48"]', text="Bike Lane Probability")
        box.prop(mod, '["Socket_49"]', text="Bike Lane Seed")
        
        
        # Material Panel
        row = box.row()
        row.label(text="Bike Lane Material:", icon='MATERIAL')
        row.prop_search(mod, '["Socket_50"]', bpy.data, "materials", text="")
        
                                        
        box.prop(mod, '["Socket_49"]', text="Bike Lane Seed")
        # Simple Label
        box.label(text="Parking Cars Settings")
        box.prop(mod, '["Socket_51"]', text="Parking Cars Distance")
        box.prop(mod, '["Socket_52"]', text="Delete Cars Probability")
        box.prop(mod, '["Socket_53"]', text="Delete Cars Seed")
        
        # Simple Label
        box.label(text="Intersection Grid Settings")
        row = box.row()
        row.scale_y = 1.5
        row.operator("mesh.set_intersection_grid", text="Add Intersection Grid", icon='VERTEXSEL').value = 1
        row.operator("mesh.set_intersection_grid", text="Remove Intersection Grid", icon='VERTEXSEL').value = 0

        # Material Panel
        row = box.row()
        row.label(text="Intersection Grid Material:", icon='MATERIAL')
        row.prop_search(mod, '["Socket_54"]', bpy.data, "materials", text="")
        
        box.prop(mod, '["Socket_55"]', text="Intersection Grid Additional Radius")
        box.prop(mod, '["Socket_56"]', text="Intersection Grid SubDiv Level")
        
        
        
        
        # Simple Label
        box.label(text="Side Walk Settings")
        # Material Panel
        row = box.row()
        row.label(text="Side Walk Material:", icon='MATERIAL')
        row.prop_search(mod, '["Socket_59"]', bpy.data, "materials", text="")
        box.prop(mod, '["Socket_60"]', text="UV Scalel")
        
        
        row = box.row()
        row = box.row()
        box.label(text="Street Light Settings")
        row = box.row()
        row = box.row()
        # Colection Panel
        row = box.row()
        row.prop_search(mod, '["Socket_62"]', bpy.data, "collections", text="Street Light Collection", icon='OUTLINER_COLLECTION')
        
        box.prop(mod, '["Socket_63"]', text="Spot Light")
        box.prop(mod, '["Socket_64"]', text="Street Lights")
        box.prop(mod, '["Socket_65"]', text="Cycles Optimised Street Lighting")
        box.prop(mod, '["Socket_66"]', text="Street Lights Corner Distance")
        box.prop(mod, '["Socket_67"]', text="Street Lights Distance")
        box.prop(mod, '["Socket_68"]', text="Street Lights Placement")
        
        
        row = box.row()
        row = box.row()
        box.label(text="Sidewalk Assets")
        row = box.row()
        
        # Colection Panel
        row = box.row()
        row.prop_search(mod, '["Socket_69"]', bpy.data, "collections", text="Side Walk Assets", icon='OUTLINER_COLLECTION')
        row = box.row()
        row.prop_search(mod, '["Socket_70"]', bpy.data, "collections", text="Secondary Side Walk Assets", icon='OUTLINER_COLLECTION')
        box.prop(mod, '["Socket_71"]', text="Use Secondary Assets")
        box.prop(mod, '["Socket_72"]', text="Probability")
        box.prop(mod, '["Socket_73"]', text="Asset Distance")
        box.prop(mod, '["Socket_74"]', text="Edge Selection")
        
        box.label(text="Railings")
        # Colection Panel
        row = box.row()
        row.prop_search(mod, '["Socket_75"]', bpy.data, "collections", text="", icon='OUTLINER_COLLECTION')
        row = box.row()
        box.prop(mod, '["Socket_76"]', text="Corner Distance")
        box.prop(mod, '["Socket_77"]', text="Length")
        box.prop(mod, '["Socket_78"]', text="Edge Selection")
        box.prop(mod, '["Socket_79"]', text="Seed")
        box.prop(mod, '["Socket_80"]', text="Delete Elements")
        box.prop(mod, '["Socket_81"]', text="Seed")
        
        
        box.label(text="Traffic Light Settings")
        # Colection Panel
        row = box.row()
        row.prop_search(mod, '["Socket_82"]', bpy.data, "collections", text="Traffic Light Collection", icon='OUTLINER_COLLECTION')
        row = box.row()
        box.prop(mod, '["Socket_83"]', text="Instance Probability")
        box.prop(mod, '["Socket_84"]', text="Seed")
        box.prop(mod, '["Socket_85"]', text="Corner Placement")
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
class CG_Traffic_Sim_Panel(bpy.types.Panel):
    bl_label = "Traffic Simulation"
    bl_idname = "CG_Traffic_Sim_Panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_parent_id = 'CG_Setting_panel'
    bl_options = {'DEFAULT_CLOSED'}
    
    def draw(self, context):
        layout = self.layout
        
        # Access the active object
        obj = context.object
        
        if obj and obj.modifiers:
            mod = obj.modifiers.get("City_Generator_2.0")
            
            if mod:
                # Create a new box for better visual grouping
                box = layout.box()
        box.operator("object.simulation_nodes_cache_calculate_to_frame", text="Calculate to Frame").selected = True
        box.operator("object.simulation_nodes_cache_bake", text="Bake Simulation").selected = True
        box.operator("object.simulation_nodes_cache_delete", text="Delete Bake")
        box.label(text="Traffic Paths")
        box.prop(mod, '["Socket_89"]', text="Show Paths")
        box.prop(mod, '["Socket_90"]', text="Path Amount")
        box.prop(mod, '["Socket_91"]', text="Seed")
        box.label(text="Car distribution")
        box.prop(mod, '["Socket_94"]', text="Car Distance Min")
        box.prop(mod, '["Socket_95"]', text="Intersection Distance")
        box.prop(mod, '["Socket_96"]', text="Delete Cars Probability")
        box.label(text="Simulation Settings")
        box.prop(mod, '["Socket_98"]', text="Min Speed")
        box.prop(mod, '["Socket_99"]', text="Max Speed")
        box.prop(mod, '["Socket_100"]', text="Seed")
        box.prop(mod, '["Socket_101"]', text="car headlights")
        
        # Colection Panel
        row = box.row()
        row.prop_search(mod, '["Socket_102"]', bpy.data, "collections", text="Car Model", icon='OUTLINER_COLLECTION')
        
        # Colection Panel
        row = box.row()
        row.prop_search(mod, '["Socket_103"]', bpy.data, "collections", text="Front Wheels", icon='OUTLINER_COLLECTION')
        
        # Colection Panel
        row = box.row()
        row.prop_search(mod, '["Socket_104"]', bpy.data, "collections", text="Back Wheels", icon='OUTLINER_COLLECTION')
        
        # Colection Panel
        row = box.row()
        row.prop_search(mod, '["Socket_105"]', bpy.data, "collections", text="Car Lights", icon='OUTLINER_COLLECTION')
        

        box.prop(mod, '["Socket_106"]', text="Instance Seed")
        box.prop(mod, '["Socket_107"]', text="Min Scale")
        box.prop(mod, '["Socket_108"]', text="Max Scale")
        box.prop(mod, '["Socket_109"]', text="Scale Seed")
        
        # Access the material named "CityGen car material"
        material = bpy.data.materials.get("CityGen car material")
        if material is None:
            layout.label(text="Material 'CityGen car material' not found.", icon='ERROR')
            return

        if not material.use_nodes:
            layout.label(text="Material 'CityGen car material' does not use nodes.", icon='ERROR')
            return

        # Look for a ColorRamp node in the material's node tree
        color_ramp_node = None
        for node in material.node_tree.nodes:
            if node.type == 'VALTORGB':  # Node type for ColorRamp
                color_ramp_node = node
                break

        if color_ramp_node is None:
            layout.label(text="No 'Color Ramp' node found in material 'CityGen car material'.", icon='ERROR')
            return

        # Create a box for the street light color UI
        box = layout.box()
        box.label(text="Car Colors:")
        box.template_color_ramp(color_ramp_node, "color_ramp", expand=True)
        
        
        

# Add Custom Atribute
def update_customheight(self, context):
    value = context.scene.height_value
    if context.object.mode == 'EDIT' and context.object.type == 'MESH':
        bpy.ops.object.mode_set(mode='OBJECT')
        mesh = context.object.data
        for poly in mesh.polygons:
            if poly.select:
                if "Custom_Height" not in mesh.attributes:
                    mesh.attributes.new(name="Custom_Height", type='INT', domain='FACE')
                mesh.attributes["Custom_Height"].data[poly.index].value = value
        bpy.ops.object.mode_set(mode='EDIT') 
        
        
        
        
        
# Add Custom Atribute
def update_custom_facade_asset_index(self, context):
    value = context.scene.custom_facade_asset_index
    if context.object.mode == 'EDIT' and context.object.type == 'MESH':
        bpy.ops.object.mode_set(mode='OBJECT')
        mesh = context.object.data
        for poly in mesh.polygons:
            if poly.select:
                if "custom Facade Asset index" not in mesh.attributes:
                    mesh.attributes.new(name="custom Facade Asset index", type='INT', domain='FACE')
                mesh.attributes["custom Facade Asset index"].data[poly.index].value = value
        bpy.ops.object.mode_set(mode='EDIT')   
        
        
        
# Add Custom Atribute
def update_custom_ground_asset_index(self, context):
    value = context.scene.custom_ground_asset
    if context.object.mode == 'EDIT' and context.object.type == 'MESH':
        bpy.ops.object.mode_set(mode='OBJECT')
        mesh = context.object.data
        for poly in mesh.polygons:
            if poly.select:
                if "custom Ground Floor Asset index" not in mesh.attributes:
                    mesh.attributes.new(name="custom Ground Floor Asset index", type='INT', domain='FACE')
                mesh.attributes["custom Ground Floor Asset index"].data[poly.index].value = value
        bpy.ops.object.mode_set(mode='EDIT')
    
    
class MESH_OT_SetmodernBuildingAttribute(bpy.types.Operator):
    bl_idname = "mesh.set_modern_building_attribute"
    bl_label = "Set Modern Building"
    value: bpy.props.IntProperty()

    def execute(self, context):
        if context.object.mode == 'EDIT' and context.object.type == 'MESH':
            bpy.ops.object.mode_set(mode='OBJECT')
            mesh = context.object.data
            for poly in mesh.polygons:
                if poly.select:
                    if "modern building" not in mesh.attributes:
                        mesh.attributes.new(name="modern building", type='INT', domain='FACE')
                    mesh.attributes["modern building"].data[poly.index].value = self.value
            bpy.ops.object.mode_set(mode='EDIT')
        return {'FINISHED'}   
    
    
    
    
class MESH_OT_DeleteBuildingAttribute(bpy.types.Operator):
    bl_idname = "mesh.delete_building_attribute"
    bl_label = "Delete Building"
    value: bpy.props.IntProperty()

    def execute(self, context):
        if context.object.mode == 'EDIT' and context.object.type == 'MESH':
            bpy.ops.object.mode_set(mode='OBJECT')
            mesh = context.object.data
            for poly in mesh.polygons:
                if poly.select:
                    if "Delete Building" not in mesh.attributes:
                        mesh.attributes.new(name="Delete Building", type='INT', domain='FACE')
                    mesh.attributes["Delete Building"].data[poly.index].value = self.value
            bpy.ops.object.mode_set(mode='EDIT')
        return {'FINISHED'}    
    
    
    
def update_zshape_amount(self, context):
    value = context.scene.zshape
    if context.object.mode == 'EDIT' and context.object.type == 'MESH':
        bpy.ops.object.mode_set(mode='OBJECT')
        mesh = context.object.data
        for poly in mesh.polygons:
            if poly.select:
                if "Zshape Amount" not in mesh.attributes:
                    mesh.attributes.new(name="Zshape Amount", type='INT', domain='FACE')
                mesh.attributes["Zshape Amount"].data[poly.index].value = value
        bpy.ops.object.mode_set(mode='EDIT')    



def update_zshape_height(self, context):
    value = context.scene.zshape_height
    if context.object.mode == 'EDIT' and context.object.type == 'MESH':
        bpy.ops.object.mode_set(mode='OBJECT')
        mesh = context.object.data
        for poly in mesh.polygons:
            if poly.select:
                if "Zshape Height" not in mesh.attributes:
                    mesh.attributes.new(name="Zshape Height", type='INT', domain='FACE')
                mesh.attributes["Zshape Height"].data[poly.index].value = value
        bpy.ops.object.mode_set(mode='EDIT')    
        
        
        
def update_zshape_insert(self, context):
    value = context.scene.zshape_insert
    if context.object.mode == 'EDIT' and context.object.type == 'MESH':
        bpy.ops.object.mode_set(mode='OBJECT')
        mesh = context.object.data
        for poly in mesh.polygons:
            if poly.select:
                # Create a float attribute if it doesn't exist
                if "Zshape insert" not in mesh.attributes:
                    mesh.attributes.new(name="Zshape insert", type='FLOAT', domain='FACE')
                # Assign the float value to the attribute
                mesh.attributes["Zshape insert"].data[poly.index].value = value
        bpy.ops.object.mode_set(mode='EDIT')

        
        
















class CG_Building_Panel(bpy.types.Panel):
    bl_label = "Building Settings"
    bl_idname = "CG_Building_Panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_parent_id = 'CG_Setting_panel'
    bl_options = {'DEFAULT_CLOSED'}
    
    def draw(self, context):
        layout = self.layout
        scene = context.scene
        
        # Access the active object
        obj = context.object
        
        if obj and obj.modifiers:
            mod = obj.modifiers.get("City_Generator_2.0")
            
            if mod:
                # Create a new box for better visual grouping
                box = layout.box()  # Start a new visual box
                box.prop(mod, '["Socket_112"]', text="Building Height Min")
                box.prop(mod, '["Socket_113"]', text="Building Height Max")
                box.prop(mod, '["Socket_114"]', text="Seed")
                
                box.prop(mod, '["Socket_120"]', text="Switch Asset Type")

                # Add Asset Seed after the box
                box.prop(mod, '["Socket_115"]', text="Asset Seed")
                
        
        
        
        

class CG_Building_Advanced_Panel(bpy.types.Panel):
    bl_label = "Advanced Settings"
    bl_idname = "CG_Building_Advanced_Panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_parent_id = 'CG_Building_Panel'
    bl_options = {'DEFAULT_CLOSED'}

    def draw(self, context):
        layout = self.layout
        scene = context.scene

        # Access the active object
        obj = context.object

        if obj and obj.modifiers:
            mod = obj.modifiers.get("City_Generator_2.0")

            if mod:
                # Initialize a box layout for visual grouping
                box = layout.box()  # You must define this before using it

                # Add Custom Height Value to the same box
                row = box.row(align=True)  # Add a row within the box
                row.scale_y = 1.5
                row.label(icon='FACESEL')  # Add the face icon
                row.prop(scene, "height_value", text="Custom Height Value")

                # Add Custom Facade Asset Index
                row = box.row(align=True)  # Add a row within the box
                row.scale_y = 1.5
                row.label(icon='FACESEL')  # Add the face icon
                row.prop(scene, "custom_facade_asset_index", text="Custom Facade Asset")

                # Add Custom Ground Floor Asset Index
                row = box.row(align=True)  # Add a row within the box
                row.scale_y = 1.5
                row.label(icon='FACESEL')  # Add the face icon
                row.prop(scene, "custom_ground_asset", text="Custom Ground Floor Asset")

                # Add operators for modern building assignment
                row = box.row()
                row.scale_y = 1.5
                row.operator("mesh.set_modern_building_attribute", text="Assign Modern Building", icon='FACESEL').value = 1
                row.operator("mesh.set_modern_building_attribute", text="Remove Modern Building", icon='FACESEL').value = 0

                # Add operators for building deletion
                row = box.row()
                row.scale_y = 1.5
                row.operator("mesh.delete_building_attribute", text="Delete Building", icon='FACESEL').value = 1
                row.operator("mesh.delete_building_attribute", text="Add Building", icon='FACESEL').value = 0
                
                
                # Add Custom Ground Floor Asset Index
                row = box.row(align=True)  # Add a row within the box
                row.scale_y = 1.5
                row.label(icon='FACESEL')  # Add the face icon
                row.prop(scene, "zshape", text="ZShape Amount")
                
                
                # Add Custom Ground Floor Asset Index
                row = box.row(align=True)  # Add a row within the box
                row.scale_y = 1.5
                row.label(icon='FACESEL')  # Add the face icon
                row.prop(scene, "zshape_height", text="ZShape Height")
                
                # Add Custom Ground Floor Asset Index
                row = box.row(align=True)  # Add a row within the box
                row.scale_y = 1.5
                row.label(icon='FACESEL')  # Add the face icon
                row.prop(scene, "zshape_insert", text="ZShape Insert")
                

            
            

class CG_Building_Asset_distribution_Panel(bpy.types.Panel):
    bl_label = "Asset Distribution Settings"
    bl_idname = "CG_Building_Asset_Distribution_Panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_parent_id = 'CG_Building_Advanced_Panel'
    bl_options = {'DEFAULT_CLOSED'}
    
    def draw(self, context):
        layout = self.layout
        obj = context.object
        
        if obj and obj.modifiers:
            mod = obj.modifiers.get("City_Generator_2.0")
            
            if mod:
                # Create a new box for better grouping
                box = layout.box()  
                box.prop(mod, '["Socket_116"]', text="Asset Layer Seed")       
                box.prop(mod, '["Socket_117"]', text="Horizontal | Vertical Order")       
                box.prop(mod, '["Socket_118"]', text="Seed")
                box.prop(mod, '["Socket_119"]', text="Mask Top Bottom Floors")
                   

        
class CG_Building_Floor_Plan_Shape_Panel(bpy.types.Panel):
    bl_label = "Floor Plan Shape Settings"
    bl_idname = "CG_Building_Floor_Plan_Panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_parent_id = 'CG_Building_Advanced_Panel'
    bl_options = {'DEFAULT_CLOSED'}
    
    def draw(self, context):
        layout = self.layout
        obj = context.object
        
        if obj and obj.modifiers:
            mod = obj.modifiers.get("City_Generator_2.0")
            
            if mod:
                # Create a new box for better grouping
                box = layout.box()
                box.prop(mod, '["Socket_122"]', text="Randomise Shape")      
                box.prop(mod, '["Socket_123"]', text="Probability")      
                box.prop(mod, '["Socket_124"]', text="Seed")      
                box.prop(mod, '["Socket_125"]', text="SubDiv Level")      
                box.prop(mod, '["Socket_126"]', text="Offset Scale")      




class CG_Building_Additional_Assets_Panel(bpy.types.Panel):
    bl_label = "Additional Assets"
    bl_idname = "CG_Building_Additional_Assets_Panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_parent_id = 'CG_Building_Advanced_Panel'
    bl_options = {'DEFAULT_CLOSED'}
    
    def draw(self, context):
        layout = self.layout
        obj = context.object
        
        if obj and obj.modifiers:
            mod = obj.modifiers.get("City_Generator_2.0")
            
            if mod:
                # Create a new box for better grouping
                box = layout.box()
                # Colection Panel
                row = box.row()
                row.prop_search(mod, '["Socket_128"]', bpy.data, "collections", text="Fire Escapes Assets", icon='OUTLINER_COLLECTION')
                row = box.row()
                box.prop(mod, '["Socket_129"]', text="Probability") 
                box.prop(mod, '["Socket_130"]', text="Seed") 
                # Colection Panel
                row = box.row()
                row.prop_search(mod, '["Socket_131"]', bpy.data, "collections", text="Flag Signs", icon='OUTLINER_COLLECTION')
                row = box.row()
                box.prop(mod, '["Socket_132"]', text="Probability")
                box.prop(mod, '["Socket_133"]', text="Seed")
                box.prop(mod, '["Socket_134"]', text="Floor Max")
                box.prop(mod, '["Socket_135"]', text="Seed")
                box.prop(mod, '["Socket_136"]', text="Scale Seed")
                box.prop(mod, '["Socket_137"]', text="Position Seed")
                row = box.row()
                row.prop_search(mod, '["Socket_138"]', bpy.data, "collections", text="Scaffolding Assets", icon='OUTLINER_COLLECTION')
                row = box.row()
                box.prop(mod, '["Socket_139"]', text="Select Edges")
                box.prop(mod, '["Socket_140"]', text="Seed")




class CG_Building_Roof_Panel(bpy.types.Panel):
    bl_label = "Roof Settings"
    bl_idname = "CG_Building_Roof_Assets_Panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_parent_id = 'CG_Building_Advanced_Panel'
    bl_options = {'DEFAULT_CLOSED'}
    
    def draw(self, context):
        layout = self.layout
        obj = context.object
        
        if obj and obj.modifiers:
            mod = obj.modifiers.get("City_Generator_2.0")
            
            if mod:
                # Create a new box for better grouping
                box = layout.box()
                row = box.row()
                row.label(text="Roof Material: ", icon='MATERIAL')
                row.prop_search(mod, '["Socket_147"]', bpy.data, "materials", text="")
                row = box.row()
                row.label(text="Roof Material: ", icon='MATERIAL')
                row.prop_search(mod, '["Socket_148"]', bpy.data, "materials", text="")
                row = box.row()
                row.label(text="Roof Material: ", icon='MATERIAL')
                row.prop_search(mod, '["Socket_149"]', bpy.data, "materials", text="")
                row = box.row()
                box.prop(mod, '["Socket_164"]', text="Seed")
                box.prop(mod, '["Socket_174"]', text="Distance Min")
                box.prop(mod, '["Socket_175"]', text="Density Factor")
                box.prop(mod, '["Socket_176"]', text="Distance From Edge")
                box.prop(mod, '["Socket_177"]', text="Scale Min")
                box.prop(mod, '["Socket_178"]', text="Scale Max")
                box.prop(mod, '["Socket_179"]', text="Seed")
                row = box.row()
                row.prop_search(mod, '["Socket_180"]', bpy.data, "collections", text="Asset Type 1", icon='OUTLINER_COLLECTION')
                row = box.row()
                row.prop_search(mod, '["Socket_181"]', bpy.data, "collections", text="Asset Type 2", icon='OUTLINER_COLLECTION')


                
                
# Update function for emission strength
def update_emission_settings(self, context):
    # List of materials to update
    material_names = [
        "CityGen_Red_Emission",
        "CityGen_Green_Emission",
        "CityGen_Blue_Emission",
        "CityGenLamp_Emission",
        "CityGen_Yellow_Emission"
    ]

    # Get the shared emission strength value
    emission_strength = context.scene.global_emission_strength

    # Update each material's emission strength
    for mat_name in material_names:
        mat = bpy.data.materials.get(mat_name)
        if mat is not None:
            node = mat.node_tree.nodes.get("Emission")  # Assuming the node is named "Emission"
            if node is not None:
                node.inputs[1].default_value = emission_strength  # Assuming input 1 is Emission Strength


# Add custom property for global emission strength
def add_emission_properties():
    bpy.types.Scene.global_emission_strength = bpy.props.FloatProperty(
        name="Global Emission Strength",
        description="Adjust the emission strength for all CityGen emission materials",
        default=1.0,
        min=0.0,
        max=100.0,
        update=update_emission_settings
    )                
                
                
                


class CG_Night_Lighting_Panel(bpy.types.Panel):
    bl_label = "Night_Lighting Settings"
    bl_idname = "CG_Night_Lighting_Panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_parent_id = 'CG_Setting_panel'
    bl_options = {'DEFAULT_CLOSED'}

    
    def draw(self, context):
        layout = self.layout
        obj = context.object

        # Access the node group for street light color (not related to the materials, but a separate group)
        node_group = bpy.data.node_groups.get("street light color")
        if node_group is None:
            layout.label(text="Node group 'street light color' not found.", icon='ERROR')
            return

        # Look for a ColorRamp node in the node group
        color_ramp_node = None
        for node in node_group.nodes:
            if node.type == 'VALTORGB':  # Node type for ColorRamp
                color_ramp_node = node
                break

        if color_ramp_node is None:
            layout.label(text="No 'Color Ramp' node found in 'street light color'.", icon='ERROR')
            return

        # Create a box for street light color
        box = layout.box()
        box.label(text="Street Light Color:")
        box.template_color_ramp(color_ramp_node, "color_ramp", expand=True)

        # Get object modifiers (make sure object is selected)
        if obj and obj.modifiers:
            mod = obj.modifiers.get("City_Generator_2.0")  # Adjust the modifier name if needed
            if mod:
                box.prop(mod, '["Socket_64"]', text="Street Lights")
                box.prop(mod, '["Socket_65"]', text="Street Lights Cycles Optimisation")
                box.prop(mod, '["Socket_63"]', text="Spot Light")
                box.prop(mod, '["Socket_101"]', text="Car Headlights")
                row = box.row()
                box.prop(context.scene, "global_emission_strength", text="Emission Materials Strength")
                








# Update function for node inputs
def update_parallax_settings(scene, context=None):
    # List of relevant materials
    material_names = [
        "CityGen_Interior_Room_Shader",
        "CityGen_Interior_Office_Shader",
        "CityGen_Interior_Store_Shader"
    ]

    # Iterate through materials and update their node inputs
    for mat_name in material_names:
        mat = bpy.data.materials.get(mat_name)
        if mat is not None:
            node = mat.node_tree.nodes.get("Group")
            if node is not None:
                node.inputs[0].default_value = scene.room_seed
                node.inputs[1].default_value = scene.close_roller_shutter
                node.inputs[2].default_value = scene.close_curtains
                node.inputs[3].default_value = scene.curtain_shutter_seed
                node.inputs[4].default_value = scene.randomise_hue
                node.inputs[5].default_value = scene.change_hue
                node.inputs[6].default_value = scene.emission_strength
                node.inputs[7].default_value = scene.light_probability
                node.inputs[8].default_value = scene.seed


# Frame change handler to update properties
def frame_change_handler(scene):
    update_parallax_settings(scene)


def property_update_callback(self, context):
    update_parallax_settings(context.scene)

# Add custom properties with update callbacks
def add_custom_properties():
    bpy.types.Scene.room_seed = bpy.props.FloatProperty(
        name="Room Seed",
        description="Adjust the room seed of all materials",
        default=0.0,
        min=0.0,
        max=100.0,
        update=property_update_callback
    )
    bpy.types.Scene.close_roller_shutter = bpy.props.FloatProperty(
        name="Close Roller Shutter",
        description="Adjust the roller shutter closure of all materials",
        default=0.0,
        min=0.0,
        max=1.0,
        update=property_update_callback
    )
    bpy.types.Scene.close_curtains = bpy.props.FloatProperty(
        name="Close Curtains",
        description="Adjust the curtain closure of all materials",
        default=0.5,
        min=0.0,
        max=1.0,
        update=property_update_callback
    )
    bpy.types.Scene.curtain_shutter_seed = bpy.props.FloatProperty(
        name="Curtain | Shutter Seed",
        description="Adjust the curtain or shutter seed of all materials",
        default=0.0,
        min=0.0,
        max=100.0,
        update=property_update_callback
    )
    bpy.types.Scene.randomise_hue = bpy.props.FloatProperty(
        name="Randomise Hue",
        description="Randomise the hue of all materials",
        default=0.0,
        min=0.0,
        max=1.0,
        update=property_update_callback
    )
    bpy.types.Scene.change_hue = bpy.props.FloatProperty(
        name="Change Hue",
        description="Adjust the hue change of all materials",
        default=0.5,
        min=-1.0,
        max=1.0,
        update=property_update_callback
    )
    bpy.types.Scene.emission_strength = bpy.props.FloatProperty(
        name="Emission Strength",
        description="Adjust the emission strength of all materials",
        default=0.0,
        min=0.0,
        max=100.0,
        update=property_update_callback
    )
    bpy.types.Scene.light_probability = bpy.props.FloatProperty(
        name="Light Probability",
        description="Adjust the light probability of all materials",
        default=0.5,
        min=0.0,
        max=1.0,
        update=property_update_callback
    )
    bpy.types.Scene.seed = bpy.props.FloatProperty(
        name="Seed",
        description="Adjust the seed of all materials",
        default=0.0,
        min=0.0,
        max=100.0,
        update=property_update_callback
    )


# Register frame change handler
def register_handlers():
    bpy.app.handlers.frame_change_pre.append(frame_change_handler)


# UI Panel in the Sidebar
class InteriorPanel(bpy.types.Panel):
    bl_label = "Interior Settings"
    bl_idname = "SCENE_PT_parallax_settings"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'City Generator'
    bl_parent_id = 'CG_Night_Lighting_Panel'
    bl_options = {'DEFAULT_CLOSED'}

    def draw(self, context):
        layout = self.layout
        
        # Add sliders for each property
        layout.prop(context.scene, "emission_strength", text="Emission Strength")
        layout.prop(context.scene, "light_probability", text="Light Probability")
        layout.prop(context.scene, "seed", text="Seed")
        layout.prop(context.scene, "randomise_hue", text="Randomise Hue")
        layout.prop(context.scene, "change_hue", text="Change Hue")
        layout.prop(context.scene, "room_seed", text="Room Seed")
        layout.prop(context.scene, "close_roller_shutter", text="Close Roller Shutter")
        layout.prop(context.scene, "close_curtains", text="Close Curtains")
        layout.prop(context.scene, "curtain_shutter_seed", text="Curtain | Shutter Seed")


                






from bpy.utils import register_class, unregister_class

classes = [
    CG_OT_Import_Node_Group,
    CG_OT_Apply_Node_Group,
    CG_PT_Main_Panel,
    CG_Setting_Panel,
    CG_General_Setting_Panel,
    CG_Street_Setting_Panel,
    CG_Park_Setting_Panel,
    CG_Street_Adv_Setting_Panel,
    CG_OT_Duplicate_Object,
    CG_Traffic_Sim_Panel,
    CG_Night_Lighting_Panel,
    CG_Building_Panel,
    CG_Building_Advanced_Panel,
    CG_Building_Asset_distribution_Panel,
    CG_Building_Floor_Plan_Shape_Panel,
    CG_Building_Additional_Assets_Panel,
    CG_Building_Roof_Panel,
    MESH_OT_SetLowPolyAttribute,
    MESH_OT_SetmodernBuildingAttribute,
    MESH_OT_DeleteBuildingAttribute,
    MESH_OT_AddParkAttribute,
    MESH_OT_Add_Intersection_Grid,
    MESH_OT_Delete_CrossWalk,
    MESH_OT_Add_Bus_Lane,
    MESH_OT_delete_Trees_Edge,
    InteriorPanel
]

def register():
    for cls in classes:
        register_class(cls)

    # Custom attributes
    bpy.types.Scene.height_value = bpy.props.IntProperty(
        name="Height Value",
        default=0,
        min=0,
        max=1000,
        update=update_customheight
    )

    bpy.types.Scene.custom_facade_asset_index = bpy.props.IntProperty(
        name="Custom Facade Asset Index",
        default=0,
        min=0,
        max=500,
        update=update_custom_facade_asset_index
    )

    bpy.types.Scene.custom_ground_asset = bpy.props.IntProperty(
        name="Custom Ground Floor Asset Index",
        default=0,
        min=0,
        max=500,
        update=update_custom_ground_asset_index
    )

    bpy.types.Scene.assign_low_poly = bpy.props.IntProperty(
        name="Assign Low Poly",
        default=0,
        min=0,
        max=500,
        update=update_custom_ground_asset_index
    )

    bpy.types.Scene.zshape = bpy.props.IntProperty(
        name="Zshape Amount",
        default=0,
        min=0,
        max=500,
        update=update_zshape_amount
    )

    bpy.types.Scene.zshape_height = bpy.props.IntProperty(
        name="Zshape Height",
        default=0,
        min=0,
        max=500,
        update=update_zshape_height
    )

    bpy.types.Scene.zshape_insert = bpy.props.FloatProperty(
        name="Zshape Insert",
        default=0,
        min=-0.75,
        max=500,
        subtype='DISTANCE',
        update=update_zshape_insert
    )

    add_custom_properties()
    register_handlers()

def unregister():
    for cls in reversed(classes):
        unregister_class(cls)

    # Remove handlers
    if frame_change_handler in bpy.app.handlers.frame_change_pre:
        bpy.app.handlers.frame_change_pre.remove(frame_change_handler)

    # Delete custom properties
    del bpy.types.Scene.height_value
    del bpy.types.Scene.custom_facade_asset_index
    del bpy.types.Scene.custom_ground_asset
    del bpy.types.Scene.assign_low_poly
    del bpy.types.Scene.zshape
    del bpy.types.Scene.zshape_height
    del bpy.types.Scene.zshape_insert

    del bpy.types.Scene.room_seed
    del bpy.types.Scene.close_roller_shutter
    del bpy.types.Scene.close_curtains
    del bpy.types.Scene.curtain_shutter_seed
    del bpy.types.Scene.randomise_hue
    del bpy.types.Scene.change_hue
    del bpy.types.Scene.emission_strength
    del bpy.types.Scene.light_probability
    del bpy.types.Scene.seed






if __name__ == "__main__":
    register()
