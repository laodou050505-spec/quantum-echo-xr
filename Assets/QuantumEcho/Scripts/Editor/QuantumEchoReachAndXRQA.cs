using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuantumEcho.Editor
{
    public static class QuantumEchoReachAndXRQA
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static void RunBatch()
        {
            EditorSceneManager.OpenScene(QuantumEchoTitleSceneBuilder.GameplayScenePath);
            var report = new StringBuilder(); var failures=0;
            Action<bool,string> check=(ok,label)=>{report.AppendLine((ok?"PASS ":"FAIL ")+label);if(!ok)failures++;};
            var manager=Object.FindAnyObjectByType<QuantumEchoGameManager>(FindObjectsInactive.Include);
            var player=manager.player;
            var controller=player.GetComponent<CharacterController>();
            typeof(QuantumPlayerRig).GetField("_characterController",Private).SetValue(player,controller);
            QuantumPlayerRig.ConfigureBodyCollider(controller);
            foreach(var level in manager.levels)
            {
                foreach(var other in manager.levels)other.gameObject.SetActive(other==level);
                level.gateLeft.localPosition=level.gateLeftClosedLocal+Vector3.left*1.56f;
                level.gateRight.localPosition=level.gateRightClosedLocal+Vector3.right*1.56f;
                foreach(var approach in new[]{Vector3.forward,Vector3.right,Vector3.left})
                {
                    level.core.gameObject.SetActive(true);
                    var start=level.core.position+approach*3f; start.y=level.transform.position.y+0.32f;
                    controller.enabled=false; player.transform.SetPositionAndRotation(start,Quaternion.identity);controller.enabled=true;
                    Physics.SyncTransforms();
                    for(var frame=0;frame<65;frame++){controller.Move(-approach*0.05f+Vector3.down*0.015f);Physics.SyncTransforms();}
                    // Aim away: this must work from legal collision-resolved proximity.
                    player.xrCamera.transform.SetPositionAndRotation(player.GroundPosition+Vector3.up,Quaternion.LookRotation(approach));
                    var legacyDistance=Vector3.ProjectOnPlane(player.GroundPosition-level.core.position,Vector3.up).magnitude;
                    check(QuantumInteractionReach.CanReach(player,level.core,1.8f,3.5f),level.levelCode+" crystal reachable at solid hull, approach="+approach+" centreDistance="+legacyDistance.ToString("F3"));
                    Set(manager,"_level",level);Set(manager,"_currentLevelIndex",level.levelIndex);Set(manager,"_won",false);Set(manager,"_coreCaptureProgress",0f);
                    Set(manager,"_gateOpenAmount",1f);Set(manager,"_trainAtJunction",true);Set(manager,"_trainDockProgress",1f);
                    if(level.fragments!=null)foreach(var fragment in level.fragments)if(fragment!=null)fragment.collected=true;
                    typeof(QuantumPlayerRig).GetField("<InteractHeld>k__BackingField",Private).SetValue(player,true);
                    typeof(QuantumEchoGameManager).GetMethod("UpdateCoreCapture",Private).Invoke(manager,new object[]{0.5f});
                    check((float)typeof(QuantumEchoGameManager).GetField("_coreCaptureProgress",Private).GetValue(manager)>0f,level.levelCode+" held trigger advances extraction at collision boundary");
                    typeof(QuantumEchoGameManager).GetMethod("UpdateCoreCapture",Private).Invoke(manager,new object[]{1.5f});
                    check((bool)typeof(QuantumEchoGameManager).GetField("_won",Private).GetValue(manager),level.levelCode+" held trigger completes crystal capture");
                }
            }
            foreach(var level in manager.levels)level.gameObject.SetActive(false);
            controller.enabled=false;player.transform.position=new Vector3(10f,50f,0f);controller.enabled=true;
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.name="QA target crystal";target.transform.position=new Vector3(0f,51f,0f);
            player.xrCamera.transform.SetPositionAndRotation(new Vector3(0f,51f,-3f),Quaternion.identity);
            var ownSolid=GameObject.CreatePrimitive(PrimitiveType.Cube);ownSolid.transform.SetParent(player.transform);ownSolid.transform.position=new Vector3(0f,51f,-1.5f);ownSolid.transform.localScale=Vector3.one*0.3f;
            var query=GameObject.CreatePrimitive(PrimitiveType.Sphere);query.transform.SetParent(player.transform);query.transform.position=new Vector3(0f,51f,-2f);query.transform.localScale=Vector3.one*0.2f;query.GetComponent<Collider>().isTrigger=true;
            Physics.SyncTransforms();
            check(QuantumInteractionReach.CanReach(player,target.transform,0.1f,3.5f),"aim ignores player's own solid and penetration trigger");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0f,51f,-0.8f);wall.transform.localScale=new Vector3(2f,2f,0.1f);Physics.SyncTransforms();
            check(!QuantumInteractionReach.CanReach(player,target.transform,0.1f,3.5f),"external wall still blocks aimed interaction");
            controller.enabled=false;player.transform.position=new Vector3(0f,50.04f,-1.5f);controller.enabled=true;
            player.xrCamera.transform.SetPositionAndRotation(new Vector3(0f,51f,-1.5f),Quaternion.identity);Physics.SyncTransforms();
            check(!QuantumInteractionReach.CanReach(player,target.transform,1.8f,3.5f),"nearby crystal cannot be collected through a wall");
            Object.DestroyImmediate(wall);Object.DestroyImmediate(query);Object.DestroyImmediate(ownSolid);Object.DestroyImmediate(target);
            TestXR(check);
            File.WriteAllText("Logs/QuantumEcho-ReachAndXRQA.txt",report.ToString());Debug.Log(report.ToString());
            if(failures!=0)throw new Exception("Reach/XR QA failed: "+failures);
        }
        static void Set(QuantumEchoGameManager manager,string field,object value)=>typeof(QuantumEchoGameManager).GetField(field,Private).SetValue(manager,value);

        static void TestXR(Action<bool,string> check)
        {
            var cameraObject=new GameObject("QA stereo camera");var camera=cameraObject.AddComponent<Camera>();
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/QuantumEcho/Fonts/NotoSansSC-Dynamic.asset");
            var hud=QuantumComfortObjectiveHUD.Create(camera,font);
            var panel=hud.GetComponent<RectTransform>();
            foreach(var fov in new[]{40f,60f,90f})
            {
                var half=0.1f*Mathf.Tan(fov*Mathf.Deg2Rad*0.5f);
                var leftProjection=Matrix4x4.Frustum(-half*0.8f,half*1.2f,-half*1.1f,half*0.9f,0.1f,100f);
                var rightProjection=Matrix4x4.Frustum(-half*1.2f,half*0.8f,-half*0.9f,half*1.1f,0.1f,100f);
                var leftEye=Matrix4x4.TRS(new Vector3(-0.032f,0,0),Quaternion.identity,new Vector3(1,1,-1));
                var rightEye=Matrix4x4.TRS(new Vector3(0.032f,0,0),Quaternion.identity,new Vector3(1,1,-1));
                var left=QuantumComfortObjectiveHUD.ProjectEyeSafeRect(leftProjection,leftEye);
                var right=QuantumComfortObjectiveHUD.ProjectEyeSafeRect(rightProjection,rightEye);
                var frame=Rect.MinMaxRect(-1.55f,-1.05f,1.55f,1.5f);
                var safe=Rect.MinMaxRect(Mathf.Max(frame.xMin,left.xMin,right.xMin),Mathf.Max(frame.yMin,left.yMin,right.yMin),Mathf.Min(frame.xMax,left.xMax,right.xMax),Mathf.Min(frame.yMax,left.yMax,right.yMax));
                foreach(var expanded in new[]{false,true})
                {
                    hud.SetHelpExpanded(expanded);
                    typeof(QuantumComfortObjectiveHUD).GetMethod("PlaceInsideXRRect",Private).Invoke(hud,new object[]{safe});
                    var corners=new Vector3[4];panel.GetWorldCorners(corners);
                    check(corners[1].x-safe.xMin>=0.059f && corners[1].x-safe.xMin<=0.061f,"XR HUD keeps stable left edge inset");
                    for(var eye=0;eye<2;eye++)
                    {
                        var projection=eye==0?leftProjection:rightProjection;var view=(eye==0?leftEye:rightEye).inverse;
                        var visible=corners.All(c=>{var p=projection*(view*new Vector4(c.x,c.y,c.z,1));return p.w>0 && Mathf.Abs(p.x/p.w)<=0.881f && Mathf.Abs(p.y/p.w)<=0.881f;});
                        check(visible,"asymmetric XR FOV="+fov+" eye="+eye+" expanded="+expanded+" all corners visible with margin");
                    }
                    check(safe.yMax-corners[1].y>=0.059f && safe.yMax-corners[1].y<=0.061f,"XR HUD keeps stable top edge inset");
                }
            }
            Object.DestroyImmediate(cameraObject);
        }
    }
}
