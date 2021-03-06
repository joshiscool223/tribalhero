package src.Objects.Store {
    import flash.display.Sprite;

    import src.Objects.Factories.StrongholdFactory;
    import src.Objects.Factories.TroopFactory;

    public class TroopStoreAsset implements IStoreAsset {
        private var item: StoreItemTheme;

        public function TroopStoreAsset(item: StoreItemTheme) {
            this.item = item;
        }

        public function title(): String {
            return t("STR_TROOPS");
        }

        public function thumbnail(): Sprite {
            return Sprite(TroopFactory.getSprite(item.themeId));
        }
    }
}
