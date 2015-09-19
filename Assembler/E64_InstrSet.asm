atomic macro num n, bytes { if (bytes > 0) { (n >> ((bytes - 1) * 8)) & 0xFF num n, bytes - 1 } }
atomic macro NOP { 0 }
atomic macro HALT { 1 }
atomic macro LOCK { 2 }
atomic macro UNLOCK { 3 }
atomic macro INT_I8 A { 4 A }
atomic macro INT_REG A { 5 A }
atomic macro JUMP_I64 A { 6 num A, 8 }
atomic macro JUMP_REG A { 7 A }
atomic macro MOVE_REG_I64 A, B { 8 A num B, 8 }
atomic macro MOVE_REG_REG A, B { 9 A B }
atomic macro PRINT_REG A { 10 A }
