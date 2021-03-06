// Copyright 2009 the Sputnik authors.  All rights reserved.
/**
 * The concat property of Array has the attribute DontEnum
 *
 * @path ch15/15.4/15.4.4/15.4.4.4/S15.4.4.4_A4.5.js
 * @description Checking use propertyIsEnumerable, for-in
 */

//CHECK#1
if (Array.propertyIsEnumerable('concat') !== false) {
  $ERROR('#1: Array.propertyIsEnumerable(\'concat\') === false. Actual: ' + (Array.propertyIsEnumerable('concat')));
}

//CHECK#2
var result = true;
for (var p in Array){
  if (p === "concat") {
    result = false;
  }  
}

if (result !== true) {
  $ERROR('#2: result = true; for (p in Array) { if (p === "concat") result = false; }  result === true;');
}


