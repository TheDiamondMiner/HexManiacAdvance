﻿using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using System.Linq;
using Xunit;

namespace HavenSoft.HexManiac.Integration {
   public class MapTests : IntegrationTests {
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
         firered.Goto.Execute("maps.3-0 Palette Town");
         var repointer = firered.MapEditor.PrimaryMap.MapRepointer;

         var text = repointer.ExpandPrimaryTilesetText;

         Assert.Equal("This primary tileset contains 640 of 640 tiles.", text);
      }
   }
}