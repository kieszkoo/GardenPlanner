CREATE TABLE TypGleby (
ID INT AUTO_INCREMENT PRIMARY KEY,
Nazwa VARCHAR(100) NOT NULL,
RetencjaWody DECIMAL(3, 2) NOT NULL,
PoziomOdzywczy DECIMAL(3, 2) NOT NULL,
Zyznosc DECIMAL(3, 2) NOT NULL
);

INSERT INTO TypGleby (Nazwa, RetencjaWody, PoziomOdzywczy, Zyznosc) VALUES
('Piaszczysta', 0.20, 0.20, 0.20),
('Gliniasta', 0.60, 0.50, 0.50),
('Lessowa', 0.80, 0.80, 0.80),
('Torfowa', 0.90, 0.40, 0.40),
('Prochnicza', 0.70, 0.90, 0.90),
('Kamienista', 0.10, 0.10, 0.10)
;

CREATE TABLE Roslina (
ID INT AUTO_INCREMENT PRIMARY KEY,
Nazwa VARCHAR(100) NOT NULL,
Tekstura2D VARCHAR(255) NOT NULL,
MaxWysokosc DECIMAL(5, 2) NOT NULL,
MaxSzerokosc DECIMAL(5, 2) NOT NULL,
GlebokoscKorzeni DECIMAL(5, 2) NOT NULL,
PromienKorony DECIMAL(5, 2) NOT NULL,
PreferencjeSlonca DECIMAL(3, 1) NOT NULL,
Typ_ID INT NOT NULL DEFAULT 1,
OkresKwitnienia INT NOT NULL DEFAULT 0
);

-- DRZEWA (Typ_ID = 1)
INSERT INTO Roslina (Nazwa, Tekstura2D, MaxWysokosc, MaxSzerokosc, GlebokoscKorzeni, PromienKorony, PreferencjeSlonca, Typ_ID, OkresKwitnienia) VALUES
('Dab szypulkowy', 'res://Textures/Plants/oak.jpg', 30.00, 20.00, 5.00, 10.00, 6.0, 1, 0),
('Sosna zwyczajna', 'res://Textures/Plants/iglak.png', 25.00, 10.00, 3.00, 5.00, 8.0, 1, 0),
('Brzoza brodawkowata', 'res://Textures/Plants/lisciaste.png', 20.00, 8.00, 2.00, 4.00, 9.0, 1, 0),
('Swierk pospolity', 'res://Textures/Plants/iglak.png', 35.00, 8.00, 2.50, 4.00, 5.0, 1, 0),
('Klon pospolity', 'res://Textures/Plants/lisciaste.png', 25.00, 15.00, 3.50, 7.50, 6.5, 1, 0),
('Lipa drobnolistna', 'res://Textures/Plants/lisciaste.png', 20.00, 15.00, 3.00, 7.50, 7.0, 1, 0),
('Buk pospolity', 'res://Textures/Plants/lisciaste.png', 30.00, 20.00, 4.00, 10.00, 4.0, 1, 0),
('Wierzba placzaca', 'res://Textures/Plants/lisciaste.png', 15.00, 10.00, 2.00, 5.00, 8.0, 1, 0),
('Grab pospolity', 'res://Textures/Plants/lisciaste.png', 20.00, 12.00, 3.00, 6.00, 3.0, 1, 0),
('Jarzab pospolity', 'res://Textures/Plants/lisciaste.png', 12.00, 6.00, 1.50, 3.00, 7.0, 1, 0),

-- KRZEWY (Typ_ID = 2)
('Roza dzika', 'res://Textures/Plants/rose.jpg', 2.50, 2.00, 1.00, 1.00, 8.0, 2, 0),
('Bez czarny', 'res://Textures/Plants/krzew.png', 5.00, 4.00, 1.50, 2.00, 6.0, 2, 0),
('Jasminowiec wonny', 'res://Textures/Plants/jasmine.png', 3.00, 2.00, 1.20, 1.00, 7.5, 2, 0),
('Lilak pospolity', 'res://Textures/Plants/krzew.png', 4.00, 3.00, 1.50, 1.50, 8.0, 2, 0),
('Forsycja posrednia', 'res://Textures/Plants/krzew.png', 2.50, 2.00, 0.80, 1.00, 8.0, 2, 0),
('Bukszpan wieczniezielony', 'res://Textures/Plants/krzew.png', 3.00, 1.50, 1.00, 0.75, 4.0, 2, 0),
('Leszczyna pospolita', 'res://Textures/Plants/krzew.png', 5.00, 4.00, 1.50, 2.00, 5.0, 2, 0),
('Trzmielina europejska', 'res://Textures/Plants/krzew.png', 3.00, 2.00, 1.00, 1.00, 6.0, 2, 0),
('Snieguliczka biala', 'res://Textures/Plants/krzew.png', 1.50, 1.50, 0.70, 0.75, 5.0, 2, 0),
('Mahonia pospolita', 'res://Textures/Plants/krzew.png', 1.00, 1.00, 0.50, 0.50, 3.0, 2, 0),

-- PAPROCIE (Typ_ID = 4)
('Paproc zwyczajna', 'res://Textures/Plants/fern.jpg', 1.00, 1.00, 0.40, 0.50, 2.0, 4, 0),
('Nerecznica samcza', 'res://Textures/Plants/paproc.png', 1.20, 0.80, 0.40, 0.40, 2.5, 4, 0),
('Wietlica samicza', 'res://Textures/Plants/paproc.png', 1.00, 0.70, 0.30, 0.35, 3.0, 4, 0),
('Pioropusznik strusi', 'res://Textures/Plants/paproc.png', 1.50, 1.00, 0.50, 0.50, 3.5, 4, 0),
('Zanokcica skalna', 'res://Textures/Plants/paproc.png', 0.30, 0.20, 0.15, 0.10, 5.0, 4, 0),

-- TRAWY (Typ_ID = 6)
('Kostrzewa sina', 'res://Textures/Plants/trawy.png', 0.30, 0.30, 0.20, 0.15, 9.0, 6, 0),
('Miskant chinski', 'res://Textures/Plants/duzetrawy.png', 2.00, 1.00, 0.60, 0.50, 8.0, 6, 0),
('Rozplenica japonska', 'res://Textures/Plants/trawy.png', 0.80, 0.80, 0.40, 0.40, 8.0, 6, 0),
('Trzcinnik piaskowy', 'res://Textures/Plants/trawy.png', 1.20, 0.50, 0.50, 0.25, 7.0, 6, 0),
('Wyczyniec lakowy', 'res://Textures/Plants/trawy.png', 0.60, 0.40, 0.30, 0.20, 6.0, 6, 0),

-- KWIATY (Typ_ID = 3)
('Stokrotka pospolita', 'res://Textures/Plants/daisy.png', 0.15, 0.15, 0.10, 0.02, 7.0, 3, 2),
('Chaber blawat', 'res://Textures/Plants/kwiatuszki.png', 0.60, 0.30, 0.25, 0.05, 8.0, 3, 3),
('Mak polny', 'res://Textures/Plants/kwiatuszki.png', 0.80, 0.40, 0.30, 0.06, 9.0, 3, 3),
('Szalkirowka (Tumbleweed)', 'res://Textures/Plants/tumbleweed.png', 0.50, 0.50, 0.20, 0.25, 10.0, 3, 0),
('Lawenda waskolistna', 'res://Textures/Plants/ziola.png', 0.60, 0.60, 0.40, 0.08, 9.0, 3, 3),
('Slonecznik zwyczajny', 'res://Textures/Plants/kwiatuszki.png', 2.50, 0.80, 0.60, 0.15, 10.0, 3, 3),
('Nagietek lekarski', 'res://Textures/Plants/ziola.png', 0.50, 0.30, 0.25, 0.05, 7.0, 3, 3),
('Aksamitka rozpierzchla', 'res://Textures/Plants/kwiatuszki.png', 0.40, 0.30, 0.20, 0.04, 8.0, 3, 3),
('Bratek ogrodowy', 'res://Textures/Plants/kwiatuszki.png', 0.20, 0.20, 0.15, 0.03, 6.0, 3, 2),
('Tulipan ogrodowy', 'res://Textures/Plants/kwiatuszki.png', 0.40, 0.20, 0.20, 0.03, 7.0, 3, 1),
('Narcyz trabkowy', 'res://Textures/Plants/kwiatuszki.png', 0.35, 0.20, 0.20, 0.03, 6.5, 3, 1),
('Piwonia chinska', 'res://Textures/Plants/kwiatuszki.png', 0.80, 0.80, 0.50, 0.10, 7.0, 3, 2),
('Mieczyk ogrodowy', 'res://Textures/Plants/kwiatuszki.png', 1.20, 0.30, 0.40, 0.05, 9.0, 3, 3),
('Dalia zmienna', 'res://Textures/Plants/kwiatuszki.png', 1.50, 1.00, 0.50, 0.12, 8.0, 3, 3),
('Zlocien wlasciwy', 'res://Textures/Plants/daisy.png', 0.70, 0.50, 0.30, 0.08, 7.0, 3, 2),
('Floks wiechowaty', 'res://Textures/Plants/kwiatuszki.png', 1.00, 0.60, 0.40, 0.09, 6.5, 3, 3),
('Rudbekia owlosiona', 'res://Textures/Plants/kwiatuszki.png', 0.80, 0.50, 0.35, 0.07, 9.0, 3, 3),
('Jezowka purpurowa', 'res://Textures/Plants/ziola.png', 1.00, 0.60, 0.45, 0.09, 8.5, 3, 3),
('Ostrózka ogrodowa', 'res://Textures/Plants/kwiatuszki.png', 1.80, 0.60, 0.50, 0.10, 7.0, 3, 3),
('Szlawia omszona', 'res://Textures/Plants/ziola.png', 0.60, 0.50, 0.35, 0.06, 9.0, 3, 2),
('Lawenda posrednia', 'res://Textures/Plants/ziola.png', 0.80, 0.80, 0.50, 0.10, 9.0, 3, 3),
('Perowskia lobolistna', 'res://Textures/Plants/ziola.png', 1.20, 1.00, 0.60, 0.12, 9.5, 3, 4),
('Krwawnik pospolity', 'res://Textures/Plants/ziola.png', 0.60, 0.40, 0.30, 0.05, 8.0, 3, 3),
('Macierzanka piaskowa', 'res://Textures/Plants/ziola.png', 0.10, 0.30, 0.15, 0.03, 9.0, 3, 3),
('Rozchodnik okazaly', 'res://Textures/Plants/kwiatuszki.png', 0.50, 0.40, 0.25, 0.06, 8.0, 3, 4),
('Gozdzik brodaty', 'res://Textures/Plants/kwiatuszki.png', 0.40, 0.30, 0.20, 0.04, 7.0, 3, 2),
('Naparstnica purpurowa', 'res://Textures/Plants/kwiatuszki.png', 1.50, 0.50, 0.40, 0.08, 5.0, 3, 3),
('Lubin trwaly', 'res://Textures/Plants/kwiatuszki.png', 1.00, 0.60, 0.80, 0.10, 7.0, 3, 2),
('Malwa rozowa', 'res://Textures/Plants/kwiatuszki.png', 2.00, 0.60, 0.50, 0.10, 8.0, 3, 3),
('Kosmos podwojnie pierzasty', 'res://Textures/Plants/kwiatuszki.png', 1.20, 0.80, 0.40, 0.10, 9.0, 3, 3),
('Cynia wytworna', 'res://Textures/Plants/kwiatuszki.png', 0.80, 0.50, 0.30, 0.07, 9.0, 3, 3),
('Nasturcja wieksza', 'res://Textures/Plants/pnacza.png', 0.30, 1.00, 0.30, 0.05, 7.0, 3, 3),
('Werbena patagonska', 'res://Textures/Plants/ziola.png', 1.50, 0.80, 0.60, 0.10, 6.5, 3, 4),
('Heliotrop peruwianski', 'res://Textures/Plants/kwiatuszki.png', 0.50, 0.40, 0.25, 0.04, 8.0, 5, 4),
('Scewola', 'res://Textures/Plants/daisy.png', 0.20, 0.80, 0.15, 0.05, 8.5, 5, 4),
('Czarnuszka damascenska', 'res://Textures/Plants/kwiatuszki.png', 0.50, 0.30, 0.20, 0.03, 7.5, 1, 4),
('Niezapominajka lesna', 'res://Textures/Plants/kwiatuszki.png', 0.25, 0.25, 0.15, 0.03, 4.0, 5, 4),
('Dzwonek skupiony', 'res://Textures/Plants/kwiatuszki.png', 0.60, 0.40, 0.30, 0.04, 7.0, 3, 4),

-- ZIOLA (Typ_ID = 5)
('Mieta pieprzowa', 'res://Textures/Plants/ziola.png', 0.60, 0.60, 0.40, 0.15, 5.0, 5, 0),
('Melisa lekarska', 'res://Textures/Plants/ziola.png', 0.80, 0.60, 0.50, 0.15, 6.0, 5, 0),
('Bazylia pospolita', 'res://Textures/Plants/ziola.png', 0.40, 0.30, 0.20, 0.10, 9.0, 5, 0),
('Rozmaryn lekarski', 'res://Textures/Plants/ziola.png', 1.00, 1.00, 0.80, 0.20, 10.0, 5, 0),
('Tymianek pospolity', 'res://Textures/Plants/ziola.png', 0.30, 0.40, 0.30, 0.10, 9.0, 5, 0),
('Szalaswia lekarska', 'res://Textures/Plants/ziola.png', 0.60, 0.60, 0.50, 0.15, 8.0, 5, 0),
('Oregano (Lebiodka)', 'res://Textures/Plants/ziola.png', 0.50, 0.50, 0.40, 0.12, 8.0, 5, 0),
('Lubczyk ogrodowy', 'res://Textures/Plants/ziola.png', 1.50, 0.80, 0.80, 0.20, 7.0, 5, 0),
('Pietruszka naciowa', 'res://Textures/Plants/ziola.png', 0.40, 0.30, 0.30, 0.10, 6.0, 5, 0),
('Koperek ogrodowy', 'res://Textures/Plants/ziola.png', 1.00, 0.40, 0.30, 0.10, 7.0, 5, 0)
;