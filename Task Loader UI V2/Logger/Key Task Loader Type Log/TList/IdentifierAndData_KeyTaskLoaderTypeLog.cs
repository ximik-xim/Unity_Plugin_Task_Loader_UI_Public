using UnityEngine; 
using TListPlugin; 
[System.Serializable]
public class IdentifierAndData_KeyTaskLoaderTypeLog : AbsIdentifierAndData<IndifNameSO_KeyTaskLoaderTypeLog, string, KeyTaskLoaderTypeLog>
{

 [SerializeField] 
 private KeyTaskLoaderTypeLog _dataKey;


 public override KeyTaskLoaderTypeLog GetKey()
 {
  return _dataKey;
 }
}
