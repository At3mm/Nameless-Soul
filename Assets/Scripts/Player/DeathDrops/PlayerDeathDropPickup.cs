using UnityEngine;

namespace World
{
    public sealed class PlayerDeathDropPickup : WorldInteractable
    {
        public string DropId { get; private set; }
        private GameEconomy.PlayerDeathDrop contents;
        private Transform visual;
        public override bool IsAvailable=>base.IsAvailable&&GameEconomy.IsCurrentDeathDrop(DropId);
        public void Configure(GameEconomy.PlayerDeathDrop drop)
        {
            contents=drop;DropId=drop.id;interactionLabel="Thu hồi "+drop.experience+" Soul/EXP và "+drop.gold+" vàng";
            var settings=PlayerDeathDropSettings.Current;interactionRadius=settings.interactionRadius;
            var icon=new GameObject("Currency marker");visual=icon.transform;visual.SetParent(transform,false);
            var renderer=icon.AddComponent<SpriteRenderer>();renderer.sprite=settings.icon;renderer.sortingLayerName=settings.sortingLayer;renderer.sortingOrder=settings.sortingOrder;
            if(settings.icon!=null)visual.localScale=Vector3.one*(settings.iconWorldHeight/Mathf.Max(.01f,settings.icon.bounds.size.y));
        }
        protected override void OnEnable(){base.OnEnable();PlayerDeathDrops.Register(this);}
        protected override void OnDisable(){PlayerDeathDrops.Unregister(this);base.OnDisable();}
        private void Update()
        {
            if(visual==null)return;
            float time=PixelAnimationSettings.Sample(Time.time);visual.localPosition=Vector3.up*(PlayerDeathDropSettings.Current.bobHeight*(1+Mathf.Sin(time*3)));
        }
        protected override bool Interact(PlayerInteractor player)
        {
            if(contents==null||player==null||!player.CanInteract||player.GetComponent<CharacterHealth>().isDead||player.gameObject.scene.path!=contents.scenePath)return false;
            if(!GameEconomy.RecoverPlayerDeathDrop(DropId)){player.ShowNotice("Chưa thể thu hồi. Hãy kiểm tra sức chứa ví.");return false;}
            MenuSFX.Play(MenuSFX.Kind.Purchase);player.ShowNotice("Đã thu hồi "+contents.experience+" Soul/EXP và "+contents.gold+" vàng.");return true;
        }
    }
}
