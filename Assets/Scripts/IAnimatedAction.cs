using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAnimatedAction
{
    IEnumerator Anticipation();
    IEnumerator Execution();
    IEnumerator Impact();
    IEnumerator Recovery();
}
