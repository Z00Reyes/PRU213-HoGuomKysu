# PRU213 - Ho Guom Ky Su

Du an game Unity phat trien trong mon PRU213, huong den mo hinh RPG nhe voi ban do dang dao, he thong vat pham, am thanh va cac thanh phan mo rong de phat trien gameplay.

## Muc tieu du an

- Xay dung bo khung game RPG co the mo rong.
- Ho tro to chuc scene, script va tai nguyen ro rang de lam viec nhom.
- Chuan hoa cau truc de tien cho viec bao tri va nop bai.

## Cong nghe su dung

- Unity (khuyen nghi mo bang Unity Hub voi version da dung trong mon hoc)
- C#
- Unity Input System
- TextMesh Pro
- Cac goi tai nguyen 2D/3D (tile, model, audio)

## Cau truc thu muc chinh

```text
Assets/
  Scenes/            # Cac scene chinh cua game
  Scripts/           # Cac script gameplay va logic
  audio/             # Nhac nen, SFX
  Model/             # Model 3D va tai nguyen lien quan
  ItemSlots/         # Tai nguyen UI/slot vat pham
  ProceduralTiles/   # Tai nguyen ho tro tao map/tile
  Settings/          # Scriptable settings va config asset
  Editor/            # Tool/Editor script phuc vu phat trien
ProjectSettings/     # Cau hinh project Unity
Packages/            # Danh sach package quan ly boi UPM
```

## Cach mo va chay du an

1. Mo Unity Hub.
2. Chon **Open** va tro den thu muc `PRU213-HoGuomKysu`.
3. Doi Unity import package va compile script lan dau.
4. Mo scene trong `Assets/Scenes/`.
5. Nhan **Play** de test.

## Quy trinh lam viec nhom de de theo doi tren Git

1. Tao branch rieng theo chuc nang: `feature/<ten-chuc-nang>`.
2. Commit theo tung nho, message ro rang.
3. Dat ten commit theo mau:
   - `feat: them he thong vat pham`
   - `fix: sua loi va cham nhan vat`
   - `chore: sap xep lai thu muc tai nguyen`
4. Tao Pull Request va ghi mo ta thay doi.
5. Sau khi merge, cap nhat README neu co thay doi lon ve kien truc.

## Quy uoc code de giam conflict

- Ten class theo PascalCase.
- Ten bien private co tien to `_` (vi du `_moveSpeed`).
- Moi script chi nen phu trach mot vai tro chinh.
- Khong hard-code duong dan asset trong code neu co the cau hinh qua Inspector.

## Tinh nang hien co (tong hop)

- Co bo khung scene va tai nguyen cho the loai RPG.
- Co Input Action map (`InputSystem_Actions.inputactions`).
- Co thanh phan map/terrain va asset phuc vu world-building.
- Co cac thu muc script/editor ho tro phat trien tiep.

## Ke hoach phat trien tiep theo

- Hoan thien dieu khien nhan vat va camera.
- Bo sung he thong inventory + item interaction.
- Tich hop NPC va logic quest co ban.
- Nang cap UI (HP, mana, thong bao nhiem vu).
- Toi uu hieu nang va to chuc lai script theo module.

## Kiem thu de xuat truoc khi nop

- [ ] Mo duoc project khong loi compile.
- [ ] Chay scene chinh khong bi null reference.
- [ ] Input nhan vat hoat dong dung nhu mong doi.
- [ ] Khong con file temp khong can thiet trong commit.
- [ ] Da cap nhat mo ta thay doi trong README/PR.

## Luu y ve tai nguyen

Du an su dung mot so asset pack de hoc tap va minh hoa. Khi public repo hoac su dung ngoai pham vi mon hoc, can kiem tra lai license cua tung goi tai nguyen.

## Thanh vien

- Nhom PRU213 - Ho Guom Ky Su
- Mon hoc: PRU213

## Lien he

Neu can trao doi ve cau truc project hoac quy trinh nop bai, tao issue trong repo de theo doi tap trung.
