namespace _Project.Scripts.Runtime.Enemies {

    public readonly struct DamageInfo {
        
        public readonly float damage;
        public readonly bool mortal;
        
        public DamageInfo(float damage, bool mortal) {
            this.damage = damage;
            this.mortal = mortal;
        }
    }
    
}