using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using UnifiedUI.Helpers;


namespace Arson
{
	/// <summary>
	/// The arson tool.
	/// </summary>
	public class ArsonTool : DefaultTool
	{
		// Cursor textures.
		private CursorInfo arsonCursor;

		/// <summary>
		/// Instance reference.
		/// </summary>
		public static ArsonTool Instance => ToolsModifierControl.toolController?.gameObject?.GetComponent<ArsonTool>();

		/// <summary>
		/// Returns true if the zoning tool is currently active, false otherwise.
		/// </summary>
		public static bool IsActiveTool => Instance != null && ToolsModifierControl.toolController.CurrentTool == Instance;


		/// <summary>
		/// Initialise the tool.
		/// Called by unity when the tool is created.
		/// </summary>
		protected override void Awake()
		{
			base.Awake();

			// Load cursors.
			arsonCursor = TextureUtils.LoadCursor("ArsonUUI.png");
			arsonCursor.m_hotspot = new Vector2(16f, 28f);
			m_cursor = arsonCursor;

			// Create new UUI button.
			UIComponent uuiButton = UUIHelpers.RegisterToolButton(
				name: nameof(ArsonTool),
				groupName: null, // default group
				tooltip: Translations.Translate("ARS_NAM"),
				tool: this,
				icon: UUIHelpers.LoadTexture(UUIHelpers.GetFullPath<ArsonMod>("Resources", "ArsonUUI.png")),
				hotkeys: new UUIHotKeys { ActivationKey = ModSettings.ToolSavedKey });
		}


		// Ignore nodes, citizens, disasters, districts, transport lines, and vehicles.
		public override NetNode.Flags GetNodeIgnoreFlags() => NetNode.Flags.All;
		public override CitizenInstance.Flags GetCitizenIgnoreFlags() => CitizenInstance.Flags.All;
		public override DisasterData.Flags GetDisasterIgnoreFlags() => DisasterData.Flags.All;
		public override District.Flags GetDistrictIgnoreFlags() => District.Flags.All;
		public override TransportLine.Flags GetTransportIgnoreFlags() => TransportLine.Flags.None;
		public override VehicleParked.Flags GetParkedVehicleIgnoreFlags() => VehicleParked.Flags.All;
		public override TreeInstance.Flags GetTreeIgnoreFlags() => TreeInstance.Flags.All;
		public override PropInstance.Flags GetPropIgnoreFlags() => PropInstance.Flags.All;
		public override Vehicle.Flags GetVehicleIgnoreFlags() => Vehicle.Flags.LeftHandDrive | Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding;


		// Select all buildings.
		public override Building.Flags GetBuildingIgnoreFlags() => Building.Flags.None;


		/// <summary>
		/// Called by the game.  Sets which network segments are ignored by the tool (always returns all, i.e. no segments are selectable by the tool).
		/// </summary>
		/// <param name="nameOnly">Always set to false</param>
		/// <returns>NetSegment.Flags.All</returns>
		public override NetSegment.Flags GetSegmentIgnoreFlags(out bool nameOnly)
		{
			nameOnly = false;
			return NetSegment.Flags.All;
		}


		/// <summary>
		/// Called by the game every simulation step.
		/// Performs raycasting to select hovered instance.
		/// </summary>
		public override void SimulationStep()
		{
			// Get base mouse ray.
			Ray mouseRay = m_mouseRay;

			// Get raycast input.
			RaycastInput input = new RaycastInput(mouseRay, m_mouseRayLength)
			{
				m_rayRight = m_rayRight,
				m_netService = GetService(),
				m_buildingService = GetService(),
				m_propService = GetService(),
				m_treeService = GetService(),
				m_districtNameOnly = Singleton<InfoManager>.instance.CurrentMode != InfoManager.InfoMode.Districts,
				m_ignoreTerrain = GetTerrainIgnore(),
				m_ignoreNodeFlags = GetNodeIgnoreFlags(),
				m_ignoreSegmentFlags = GetSegmentIgnoreFlags(out input.m_segmentNameOnly),
				m_ignoreBuildingFlags = GetBuildingIgnoreFlags(),
				m_ignoreTreeFlags = GetTreeIgnoreFlags(),
				m_ignorePropFlags = GetPropIgnoreFlags(),
				m_ignoreVehicleFlags = GetVehicleIgnoreFlags(),
				m_ignoreParkedVehicleFlags = GetParkedVehicleIgnoreFlags(),
				m_ignoreCitizenFlags = GetCitizenIgnoreFlags(),
				m_ignoreTransportFlags = GetTransportIgnoreFlags(),
				m_ignoreDistrictFlags = GetDistrictIgnoreFlags(),
				m_ignoreParkFlags = GetParkIgnoreFlags(),
				m_ignoreDisasterFlags = GetDisasterIgnoreFlags(),
				m_transportTypes = GetTransportTypes()
			};

			ToolErrors errors = ToolErrors.None;
			RaycastOutput output;

			// Is the base mouse ray valid?
			if (m_mouseRayValid)
			{
				// Yes - raycast.
				if (RayCast(input, out output))
				{
					// Create new hover instance.
					InstanceID hoverInstance = InstanceID.Empty;

					// Set base tool accurate position.
					m_accuratePosition = output.m_hitPos;

					// Check for building hits.
					if (output.m_building != 0)
					{
							// Building has eligible transfers - set hover, and set cursor to light/
							hoverInstance.Building = (ushort)output.m_building;
					}

					// Update tool hover instance.
					m_hoverInstance = hoverInstance;
				}
				else
				{
					// Raycast failed.
					errors = ToolErrors.RaycastFailed;
				}
			}
			else
			{
				// No valid mouse ray.
				output = default;
				errors = ToolErrors.RaycastFailed;
			}

			// Set mouse position and record errors.
			m_mousePosition = output.m_hitPos;
			m_selectErrors = errors;
		}


		/// <summary>
		/// Toggles the current tool to/from the ArsonTool.
		/// </summary>
		internal static void ToggleTool()
		{
			// Activate ArsonTool tool if it isn't already; if already active, deactivate it by selecting the default tool instead.
			if (!IsActiveTool)
			{
				// Activate tool.
				ToolsModifierControl.toolController.CurrentTool = Instance;
			}
			else
			{
				// Activate default tool.
				ToolsModifierControl.SetTool<DefaultTool>();
			}
		}


		/// <summary>
		/// Unity late update handling.
		/// Called by game every late update.
		/// </summary>
		protected override void OnToolLateUpdate()
		{
			base.OnToolLateUpdate();

			// Force the info mode to none.
			ToolBase.ForceInfoMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.None);
		}


		/// <summary>
		/// Tool GUI event processing.
		/// Called by game every GUI update.
		/// </summary>
		/// <param name="e">Event</param>
		protected override void OnToolGUI(Event e)
		{
			// Don't do anything if mouse is inside UI or if there are any errors other than failed raycast.
			if (m_toolController.IsInsideUI || (m_selectErrors != ToolErrors.None && m_selectErrors != ToolErrors.RaycastFailed))
			{
				return;
			}

			// Try to get a hovered building instance.
			ushort building = m_hoverInstance.Building;
			if (building != 0)
			{
				// Check for mousedown events with button zero.
				if (e.type == EventType.MouseDown && e.button == 0)
				{
					// Got one; use the event.
					UIInput.MouseUsed();

					// Start fire via simulation thread.
					Singleton<SimulationManager>.instance.AddAction(delegate
					{
						InstanceID instance = default(InstanceID);
						instance.Building = building;
						InstanceManager.Group group = Singleton<InstanceManager>.instance.GetGroup(instance);
						Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].Info.m_buildingAI.BurnBuilding(building, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[building], group, false);
					});
				}
			}
		}
	}
}
