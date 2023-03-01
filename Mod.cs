using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Linq;

namespace AlphabetizeMenus
{
    public class ModEntry: Mod
    {

        public override void Entry(IModHelper helper)
        {
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Display.MenuChanged += Display_OnMenuChanged;
        }

        // sorting crafting recipes data automatically sorts the crafting menu
        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    List<string> sortedCraftingNames = data.Keys.ToList<string>();
                    sortedCraftingNames.Sort();

                    Dictionary<string, string> sortedCraftingList = new Dictionary<string, string>();
                    foreach (string name in sortedCraftingNames)
                    {
                        sortedCraftingList.Add(name, data[name]);
                    }

                    asset.ReplaceWith(sortedCraftingList);
                }, AssetEditPriority.Late
                );
            }
        }

        // sorting recipes
        private void Display_OnMenuChanged(object sender, MenuChangedEventArgs e)
        {

            if ((!(e.NewMenu is CraftingPage)) && (!(e.NewMenu is GameMenu)))
                return;

            // for recipe menu from interacting with 
            if (e.NewMenu is CraftingPage)
            {
                var menu = (CraftingPage)e.NewMenu;
                List<string> playerRecipes = new List<string>();
                foreach (string s in CraftingRecipe.cookingRecipes.Keys)
                {
                    playerRecipes.Add(s);
                }
                playerRecipes.Sort();
                menu.pagesOfCraftingRecipes.Clear();
                Helper.Reflection.GetMethod(menu, "layoutRecipes").Invoke(playerRecipes);
            }

            // for collections tab
            else if ((e.NewMenu is GameMenu))
            {
                var menu = (GameMenu)e.NewMenu;
                if (menu.pages[5] is CollectionsPage)
                {
                    var collectionsTab = (CollectionsPage)menu.pages[5];
                    collectionsTab.collections[4] = AlphaSortCookingCollection(menu);
                    collectionsTab.collections[0] = ModSortShippingCollection(menu);
                }
            }
        }

        private static List<List<ClickableTextureComponent>> AlphaSortCookingCollection(GameMenu menu)
        {
            // get ObjectInformation data
            IDictionary<int, string> objectInformation = new Dictionary<int, string>(Game1.objectInformation);

            var menuPages = menu.pages;
            var collectionsTab = (CollectionsPage)menuPages[5];

            // cooking collection
            var cookingTab = collectionsTab.collections[4];
            int cookingPageSize = 0;
            List<ClickableTextureComponent> allCookingItems = new List<ClickableTextureComponent>();
            foreach (var page in cookingTab)
            {
                allCookingItems.AddRange(page);
                if (page.Count > cookingPageSize)
                {
                    cookingPageSize = page.Count;
                }
            }
            // sort by name instead of object id
            allCookingItems.Sort(delegate (ClickableTextureComponent a, ClickableTextureComponent b)
            {
                string name1 = "";
                string name2 = "";
                if (a != null && int.TryParse(a.name.Split(" ")[0], out var result1))
                {
                    name1 = objectInformation[result1].Split("/")[4];
                }
                if (b != null && int.TryParse(b.name.Split(" ")[0], out var result2))
                {
                    name2 = objectInformation[result2].Split("/")[4];

                }
                return name1.CompareTo(name2);
            });
            List<List<ClickableTextureComponent>> sortedCookingCollection = new List<List<ClickableTextureComponent>>();
            List<ClickableTextureComponent> newCookingPage = new List<ClickableTextureComponent>();
            int cookingPageIndex = 0;
            for (var i = 0; i < allCookingItems.Count; i++)
            {
                int indexOnPage = i % cookingPageSize;

                // steal fields to make new clickable texture component
                var targetIcon = cookingTab[cookingPageIndex][indexOnPage];
                newCookingPage.Add(new ClickableTextureComponent(allCookingItems[i].name, targetIcon.bounds, allCookingItems[i].label, allCookingItems[i].hoverText, allCookingItems[i].texture, allCookingItems[i].sourceRect, allCookingItems[i].scale, allCookingItems[i].drawShadow)
                {
                    myID = targetIcon.myID,
                    rightNeighborID = targetIcon.rightNeighborID,
                    leftNeighborID = targetIcon.leftNeighborID,
                    downNeighborID = targetIcon.downNeighborID,
                    upNeighborID = targetIcon.upNeighborID,
                    fullyImmutable = true
                });

                if (newCookingPage.Count == cookingPageSize)
                {
                    sortedCookingCollection.Add(newCookingPage);
                    cookingPageIndex++;
                    newCookingPage = new List<ClickableTextureComponent>();
                }
            }
            sortedCookingCollection.Add(newCookingPage);
            return sortedCookingCollection;
        }

        private static List<List<ClickableTextureComponent>> ModSortShippingCollection(GameMenu menu)
        {
            // get ObjectInformation data
            IDictionary<int, string> objectInformation = new Dictionary<int, string>(Game1.objectInformation);

            var menuPages = menu.pages;
            var collectionsTab = (CollectionsPage)menuPages[5];

            // shipping collection
            var objectDataModOrdered = new List<string>();
            objectDataModOrdered.AddRange(objectInformation.Values);
            var shippingTab = collectionsTab.collections[0];
            int shippingPageSize = 0;
            List<ClickableTextureComponent> allShippingItems = new List<ClickableTextureComponent>();
            foreach (var page in shippingTab)
            {
                allShippingItems.AddRange(page);
                if (page.Count > shippingPageSize)
                {
                    shippingPageSize = page.Count;
                }
            }

            // grab the sort from the ObjectInformation asset, which is actually not numerically sorted
            allShippingItems.Sort(delegate (ClickableTextureComponent a, ClickableTextureComponent b)
            {
                int num = -1;
                int value = -1;
                if (a != null && int.TryParse(a.name.Split(" ")[0], out var objectID1))
                {
                    string objectData1 = objectInformation[objectID1];
                    num = objectDataModOrdered.IndexOf(objectData1);
                }
                if (b != null && int.TryParse(b.name.Split(" ")[0], out var objectID2))
                {
                    string objectData2 = objectInformation[objectID2];
                    value = objectDataModOrdered.IndexOf(objectData2);

                }
                return num.CompareTo(value);
            });
            List<List<ClickableTextureComponent>> sortedShippingCollection = new List<List<ClickableTextureComponent>>();
            List<ClickableTextureComponent> newShippingPage = new List<ClickableTextureComponent>();
            int shippingPageIndex = 0;
            for (var i = 0; i < allShippingItems.Count; i++)
            {
                int indexOnPage = i % shippingPageSize;

                // steal fields to make new clickable texture component
                var targetIcon = shippingTab[shippingPageIndex][indexOnPage];
                newShippingPage.Add(new ClickableTextureComponent(allShippingItems[i].name, targetIcon.bounds, allShippingItems[i].label, allShippingItems[i].hoverText, allShippingItems[i].texture, allShippingItems[i].sourceRect, allShippingItems[i].scale, allShippingItems[i].drawShadow)
                {
                    myID = targetIcon.myID,
                    rightNeighborID = targetIcon.rightNeighborID,
                    leftNeighborID = targetIcon.leftNeighborID,
                    downNeighborID = targetIcon.downNeighborID,
                    upNeighborID = targetIcon.upNeighborID,
                    fullyImmutable = true
                });

                if (newShippingPage.Count == shippingPageSize)
                {
                    sortedShippingCollection.Add(newShippingPage);
                    shippingPageIndex++;
                    newShippingPage = new List<ClickableTextureComponent>();
                }
            }
            sortedShippingCollection.Add(newShippingPage);
            return sortedShippingCollection;
        }
    }
}
