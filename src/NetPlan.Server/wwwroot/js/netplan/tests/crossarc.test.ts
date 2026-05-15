/**
 * 交叉弧工具函数单元测试
 */

import { describe, test, expect } from 'vitest';
import { findSegIntersection } from '../render/crossarc.js';

describe('findSegIntersection', () => {

  test('垂直交叉 — 两线段十字相交', () => {
    const p = findSegIntersection(0, 0, 10, 0, 5, -5, 5, 5);
    expect(p).toBeDefined();
    expect(p!.x).toBe(5);
    expect(p!.y).toBe(0);
  });

  test('水平交叉 — 两线段十字相交', () => {
    const p = findSegIntersection(5, -5, 5, 5, 0, 0, 10, 0);
    expect(p).toBeDefined();
    expect(p!.x).toBe(5);
    expect(p!.y).toBe(0);
  });

  test('不交叉 — 平行线', () => {
    const p = findSegIntersection(0, 0, 5, 0, 0, 3, 5, 3);
    expect(p).toBeNull();
  });

  test('不交叉 — 共线', () => {
    const p = findSegIntersection(0, 0, 5, 0, 10, 0, 15, 0);
    expect(p).toBeNull();
  });

  test('端点相接 — T 形', () => {
    const p = findSegIntersection(0, 0, 10, 0, 5, 0, 5, -5);
    expect(p).toBeDefined();
    // 端点相接也算交叉
    expect(p!.x).toBe(5);
    expect(p!.y).toBe(0);
  });

  test('斜线交叉', () => {
    const p = findSegIntersection(0, 0, 10, 10, 0, 10, 10, 0);
    expect(p).toBeDefined();
    expect(p!.x).toBe(5);
    expect(p!.y).toBe(5);
  });

  test('不交叉 — 端点不相连的两段', () => {
    const p = findSegIntersection(0, 0, 5, 0, 6, 1, 10, 1);
    expect(p).toBeNull();
  });

});
