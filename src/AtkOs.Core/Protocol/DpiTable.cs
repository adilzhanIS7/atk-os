namespace AtkOs.Core.Protocol;

/// <summary>
/// Raw byte → DPI lookup used by sensors with max DPI ≤ 18000 (R1 SE/SE+, Star R1, ...).
/// The sensor register is not linear, so the official app uses a fixed table.
/// Entries marked CanDouble also give 2× their value (10100–18000) with ext flag 0x20.
/// </summary>
public static class DpiTable
{
    public static readonly (byte Raw, int Dpi, bool CanDouble)[] Entries =
    [
        (1, 50, false), (2, 100, false), (3, 150, false), (4, 200, false), (5, 250, false), (6, 300, false),
        (8, 350, false), (9, 400, false), (10, 450, false), (11, 500, false), (12, 550, false), (14, 600, false),
        (15, 650, false), (16, 700, false), (17, 750, false), (18, 800, false), (19, 850, false), (21, 900, false),
        (22, 950, false), (23, 1000, false), (24, 1050, false), (25, 1100, false), (27, 1150, false), (28, 1200, false),
        (29, 1250, false), (30, 1300, false), (31, 1350, false), (32, 1400, false), (34, 1450, false), (35, 1500, false),
        (36, 1550, false), (37, 1600, false), (38, 1650, false), (39, 1700, false), (41, 1750, false), (42, 1800, false),
        (43, 1850, false), (44, 1900, false), (45, 1950, false), (47, 2000, false), (48, 2050, false), (49, 2100, false),
        (50, 2150, false), (51, 2200, false), (52, 2250, false), (54, 2300, false), (55, 2350, false), (56, 2400, false),
        (57, 2450, false), (58, 2500, false), (59, 2550, false), (61, 2600, false), (62, 2650, false), (63, 2700, false),
        (64, 2750, false), (65, 2800, false), (67, 2850, false), (68, 2900, false), (69, 2950, false), (70, 3000, false),
        (71, 3050, false), (72, 3100, false), (74, 3150, false), (75, 3200, false), (76, 3250, false), (77, 3300, false),
        (78, 3350, false), (79, 3400, false), (81, 3450, false), (82, 3500, false), (83, 3550, false), (84, 3600, false),
        (85, 3650, false), (87, 3700, false), (88, 3750, false), (89, 3800, false), (90, 3850, false), (91, 3900, false),
        (92, 3950, false), (94, 4000, false), (95, 4050, false), (96, 4100, false), (97, 4150, false), (98, 4200, false),
        (99, 4250, false), (101, 4300, false), (102, 4350, false), (103, 4400, false), (104, 4450, false), (105, 4500, false),
        (107, 4550, false), (108, 4600, false), (109, 4650, false), (110, 4700, false), (111, 4750, false), (112, 4800, false),
        (114, 4850, false), (115, 4900, false), (116, 4950, false), (117, 5000, false), (118, 5050, true), (119, 5100, true),
        (121, 5150, true), (122, 5200, true), (123, 5250, true), (124, 5300, true), (125, 5350, true), (127, 5400, true),
        (128, 5450, true), (129, 5500, true), (130, 5550, true), (131, 5600, true), (132, 5650, true), (134, 5700, true),
        (135, 5750, true), (136, 5800, true), (137, 5850, true), (138, 5900, true), (139, 5950, true), (141, 6000, true),
        (142, 6050, true), (143, 6100, true), (144, 6150, true), (145, 6200, true), (147, 6250, true), (148, 6300, true),
        (149, 6350, true), (150, 6400, true), (151, 6450, true), (152, 6500, true), (154, 6550, true), (155, 6600, true),
        (156, 6650, true), (157, 6700, true), (158, 6750, true), (159, 6800, true), (161, 6850, true), (162, 6900, true),
        (163, 6950, true), (164, 7000, true), (165, 7050, true), (167, 7100, true), (168, 7150, true), (169, 7200, true),
        (170, 7250, true), (171, 7300, true), (172, 7350, true), (174, 7400, true), (175, 7450, true), (176, 7500, true),
        (177, 7550, true), (178, 7600, true), (179, 7650, true), (181, 7700, true), (182, 7750, true), (183, 7800, true),
        (184, 7850, true), (185, 7900, true), (187, 7950, true), (188, 8000, true), (189, 8050, true), (190, 8100, true),
        (191, 8150, true), (192, 8200, true), (194, 8250, true), (195, 8300, true), (196, 8350, true), (197, 8400, true),
        (198, 8450, true), (199, 8500, true), (201, 8550, true), (202, 8600, true), (203, 8650, true), (204, 8700, true),
        (205, 8750, true), (207, 8800, true), (208, 8850, true), (209, 8900, true), (210, 8950, true), (211, 9000, true),
        (212, 9050, false), (214, 9100, false), (215, 9150, false), (216, 9200, false), (217, 9250, false), (218, 9300, false),
        (219, 9350, false), (221, 9400, false), (222, 9450, false), (223, 9500, false), (224, 9550, false), (225, 9600, false),
        (227, 9650, false), (228, 9700, false), (229, 9750, false), (230, 9800, false), (231, 9850, false), (232, 9900, false),
        (234, 9950, false), (235, 10000, false),
    ];
}
