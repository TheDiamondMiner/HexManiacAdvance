﻿using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels.DataFormats;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using System;
using System.Linq;
using Xunit;

namespace HavenSoft.HexManiac.Integration {
   public class MapTests : IntegrationTests {
      private const string StartTown = "maps.3-0 Pallet Town";

      [SkippableFact]
      public void NoConnections_CreateNewMap_AddsConnectionTable() {
         var firered = LoadFireRed();
         firered.Goto.Execute("maps.1-0 Viridian Forest");
         var leftEdgeButton = firered.MapEditor.MapButtons.Single(button => button.Icon == MapSliderIcons.ExtendLeft);

         var newMapButton = leftEdgeButton.ContextItems.Single(item => item.Text == "Create New Map");
         newMapButton.Execute();

         var forest = firered.Model.GetTableModel("data.maps.banks/1/maps/0/map/");
         Assert.Equal(1, forest[0].GetSubTable("connections")[0].GetValue("count"));
      }

      [SkippableFact]
      public void MapRepointer_ExpandPrimaryTilesetText_HasMaxTiles() {
         var firered = LoadReadOnlyFireRed();
         firered.Goto.Execute(StartTown);
         var repointer = firered.MapEditor.PrimaryMap.MapRepointer;

         var text = repointer.ExpandPrimaryTilesetText;

         Assert.Equal("This primary tileset contains 640 of 640 tiles.", text);
      }

      [SkippableFact]
      public void FlowerBlock_EditBlockLayer_OneByteChangeInFile() {
         var firered = LoadFireRed();
         firered.Goto.Execute(StartTown);

         firered.MapEditor.SelectBlock(0, 4);
         firered.MapEditor.ReleaseBlock(0, 4);
         firered.MapEditor.PrimaryMap.BlockEditor.Layer = 2;

         firered.Diff.Execute();
         Assert.Equal("1 changes found.", Messages.Single());
      }

      [SkippableFact]
      public void EditBorderBlock_SelectBlock_UpdateSelectedBlock() {
         var firered = LoadReadOnlyFireRed();
         firered.Goto.Execute(StartTown);
         var view = new StubView(firered.MapEditor);

         firered.MapEditor.ReadBorderBlock(4, 3);

         Assert.Contains(nameof(MapEditorViewModel.BlockSelectionToggle), view.PropertyNotifications);
         Assert.True(firered.MapEditor.BlockEditorVisible);
         Assert.Contains(nameof(firered.MapEditor.AutoscrollBlocks), view.EventNotifications);
      }

      [SkippableFact]
      public void CleanFile_OpenMapEditor_VisibleMapsContainsPrimaryMap() {
         var firered = LoadReadOnlyFireRed();

         firered.Goto.Execute(StartTown);

         var editor = firered.MapEditor;
         Assert.Contains(editor.PrimaryMap, editor.VisibleMaps);
      }

      [SkippableFact]
      public void OakLabSignpost_RepointScript_UpdateSignpostScriptField() {
         var firered = LoadFireRed();
         var address1 = firered.Maps[3][0].Events.Signposts[0].Arg;

         firered.Goto.Execute(StartTown);
         firered.MapEditor.PrimaryMap.EventGroup.Signposts[0].GotoScript();
         var script = firered.Tools.CodeTool.Contents[0];
         script.Content = "nop" + Environment.NewLine + script.Content; // causes repoint

         var address2 = firered.Maps[3][0].Events.Signposts[0].Arg;
         Assert.NotEqual(address1, address2);
      }

      [SkippableFact]
      public void AddMapScript_Undo_NoMetadataIssues() {
         var firered = LoadFireRed();
         firered.Goto.Execute(StartTown);
         firered.MapEditor.PrimaryMap.MapScriptCollection.AddScript();
         firered.MapEditor.PrimaryMap.MapScriptCollection.Scripts[2].ScriptTypeIndex = 3;

         while (!firered.ChangeHistory.IsSaved)
            firered.Undo.Execute();

         AssertNoConflicts(firered);
      }

      [SkippableFact]
      public void MapsWithSameLayout_RepointViaTableTool_NewLayout() {
         var firered = LoadFireRed();
         firered.Goto.Execute("data.maps.banks/5/maps/4/map/0/"); // viridian city pokemon center

         var group = (StreamElementViewModel)firered.Tools.TableTool.Groups[1].Members[0];
         group.Repoint.Execute();

         var layouts = firered.Model.GetTableModel("data.maps.layouts");
         Assert.Equal(384, layouts.Count);
      }

      [SkippableFact]
      public void SelectBlock_SelectEvent_NoBlockSelected() {
         var firered = LoadReadOnlyFireRed();
         firered.Goto.Execute(StartTown);
         firered.MapEditor.SelectBlock(2, 2);
         firered.MapEditor.ReleaseBlock(2, 2);

         var ev = firered.MapEditor.PrimaryMap.EventGroup.Objects[0];
         firered.MapEditor.EventDown(ev, PrimaryInteractionStart.None);
         firered.MapEditor.EventUp();

         Assert.False(firered.MapEditor.BlockEditorVisible);
      }

      [SkippableFact]
      public void Door_CreateNewMap_NewMapWarpConnectsToSourceWarp() {
         var firered = LoadFireRed();
         firered.Goto.Execute(StartTown);
         var warp = firered.MapEditor.PrimaryMap.EventGroup.Warps[1];

         FileSystem.ShowOptions = (_, _, _, _) => 0;
         firered.MapEditor.CreateMapForWarp(warp);

         warp = firered.MapEditor.PrimaryMap.EventGroup.Warps[0];
         Assert.Equal(2, warp.WarpID);
      }

      [SkippableFact]
      public void CreateNewMapFromWarp_Undo_NoError() {
         int close = 0;
         FileSystem.ShowOptions = (_, _, _, _) => 0;
         var firered = LoadFireRed();
         firered.Goto.Execute(StartTown);
         firered.MapEditor.CreateMapForWarp(firered.MapEditor.PrimaryMap.EventGroup.Warps[1]);
         firered.MapEditor.Closed += (sender, e) => close++;

         firered.MapEditor.Undo.Execute();

         Assert.Single(Errors);
         Assert.Equal(1, close);
      }

      [SkippableFact]
      public void CreateNewMapFromConnection_Undo_NoError() {
         FileSystem.ShowOptions = (_, _, _, _) => 0;
         var firered = LoadFireRed();
         firered.Goto.Execute(StartTown);
         firered.MapEditor.PrimaryMap.ConnectNewMap(new(10, 0, MapDirection.Right));
         firered.MapEditor.PrimaryMap = firered.MapEditor.VisibleMaps.Last(); // should be the map that was just inserted

         firered.MapEditor.Undo.Execute();

         Assert.Equal(3000, firered.MapEditor.PrimaryMap.MapID);
      }

      [SkippableFact]
      public void FireRed_LoadStartingMap_SignpostsLoadCorrectly() {
         var firered = LoadReadOnlyFireRed();
         var table = firered.Model.GetTable("data.maps.banks/3/maps/0/map/0/events/");
         var format = (Pointer)table.CreateDataFormat(firered.Model, table.Start + table.Length - 1);
         Assert.False(format.HasError);
      }
   }
}
