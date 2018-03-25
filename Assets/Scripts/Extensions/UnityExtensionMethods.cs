using UnityEngine;

namespace Assets.Scripts.Extensions
{
    public static class UnityExtensionMethods
    {

        #region Transform Extensions
        
        public static void ResetTransformation(this Transform trans)
        {
            trans.position = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale = new Vector3(1, 1, 1);
        }
        
        #endregion


        #region Game Object Extensions

        public static GameObject SetName(this GameObject gameObject, string name)
        {
            gameObject.name = name;
            return gameObject;
        }

        public static GameObject SetPosition(this GameObject gameObject, float x, float y, float z = 0)
        {
            gameObject.transform.position = new Vector3(x, y, z);
            return gameObject;
        }

        public static GameObject ResetTransformation(this GameObject gameObject)
        {
            gameObject.transform.ResetTransformation();
            return gameObject;
        }
        
        #endregion
    }
}