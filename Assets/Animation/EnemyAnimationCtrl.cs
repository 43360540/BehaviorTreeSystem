using UnityEngine;

public class EnemyAnimationCtrl : MonoBehaviour
{
    [SerializeField] private Animator _anim;

    private readonly int _isAttacking = Animator.StringToHash("IsAttacking");

    private void AttackEnter()
    {
        _anim.SetBool(_isAttacking, true);
    }

    private void AttackExit()
    {
        _anim.SetBool(_isAttacking, false);
    }
}
