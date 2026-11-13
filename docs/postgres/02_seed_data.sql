--
-- PostgreSQL database dump
--


-- Dumped from database version 18.2
-- Dumped by pg_dump version 18.2

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Data for Name: accounts; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public.accounts VALUES ('2623abf2-cc14-4352-bd69-21336ddd6160', 'Fgt2', 'Fgt2', 'SN9RfU+xTTP1pWQYzolj5pXTmB2EDdh9P/rdSGM7pAA=', 'zhP6ea3ywL+U+mMtfcD0wg==', 973, 'NorthernRealms', 'wolf', 0, 2, 0, 2, 0, 0, 0, 0, '2026-10-27 18:29:19.929832+03', '2026-10-27 18:40:30.095075+03', 'Player', '2026-10-28 18:30:10.365831+03');
INSERT INTO public.accounts VALUES ('48196829-bc0b-47f3-bf5b-b44f8b71a8d9', 'admin', 'Admin', 'W/j51kx1qlhPblz76fTjGqUI6gtiQLn1p+57ojVo10o=', 'QbK7EwMn7QTZDcjTai7RRg==', 1059, 'NorthernRealms', 'wolf', 0, 4, 4, 0, 0, 2, 2, 0, '2026-10-23 19:49:07.989735+03', '2026-11-02 18:00:45.181064+03', 'Admin', NULL);
INSERT INTO public.accounts VALUES ('8fb5448b-2df9-42a8-b2c5-0ff8f657136d', 'fgt3', 'fgt3', 'H8fjBzO6j6aEDdXv+nmOkGUnUZpaHhdkCsqodurzEW0=', 'UDmZpGMB68VEXKVeUi/8Gg==', 1000, 'NorthernRealms', 'wolf', 0, 0, 0, 0, 0, 0, 0, 0, '2026-10-31 14:25:01.664963+03', '2026-10-31 14:25:01.664965+03', 'Player', NULL);
INSERT INTO public.accounts VALUES ('c2ea4966-a250-47dc-92a4-146c16b0806d', 'Fgt1', 'Fgt1', 'DiCaWRaJJh0jRYczyg0uvmARtifRwWfPxmI8kp3l07s=', '5xq4ZZYgNcFiv4RaqYs80A==', 971, 'NorthernRealms', 'wolf', 0, 2, 0, 2, 0, 0, 0, 0, '2026-10-27 18:05:02.595359+03', '2026-10-27 18:28:49.12452+03', 'Player', '2026-11-26 18:05:18.748264+03');
INSERT INTO public.accounts VALUES ('edbbb339-39c2-44e9-89fb-5012f536630c', 'aaa', 'aaa', '8bv1UyCfpcE6D/Ie3xM+P3i6SpvdI3MOLMcgjimrDsg=', 'nMLQnckglHZU2ya+pBkXwQ==', 1000, 'NorthernRealms', 'wolf', 0, 0, 0, 0, 0, 0, 0, 0, '2026-10-24 14:08:39.368152+03', '2026-10-24 14:08:59.623283+03', 'Player', '2026-11-23 14:20:15.759349+03');


--
-- Data for Name: decks; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public.decks VALUES ('1d06d078-67a2-4fb2-a088-37ed91f175a8', 'Mine', 'NorthernRealms', 'nr_leader_foltest_siege', '["nr_vernon_roche", "nr_john_natalis", "nr_esterad", "nr_philippa", "nr_ves", "nr_yarpen", "nr_keira", "nr_dandelion", "nr_sile", "nr_prince_stennis", "nr_thaler", "nr_dun_banner", "nr_sabrina", "nr_sheldon", "nr_crinfrid1", "nr_crinfrid2", "nr_crinfrid3", "nr_blue_stripes1", "nr_blue_stripes2", "nr_blue_stripes3", "nr_catapult1", "nr_catapult2", "nr_ballista", "nr_trebuchet1", "nr_trebuchet2", "nr_siege_tower", "nr_poor_infantry1", "nr_poor_infantry2", "nr_poor_infantry3", "nr_kaedweni1", "nr_kaedweni2", "nr_kaedweni3", "n_geralt", "n_yennefer", "n_villentretenmerth", "n_decoy1", "n_horn1", "n_frost", "n_clear", "n_scorch"]', '48196829-bc0b-47f3-bf5b-b44f8b71a8d9', '2026-11-13 09:18:26.009103+03');


--
-- Data for Name: invite_requests; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public.invite_requests VALUES ('6d56eeb19746746d', '2623abf2-cc14-4352-bd69-21336ddd6160', 'Fgt2', 'Fgt2', 1, 'Approved', '2026-10-27 18:29:53.089661+03', '2026-10-27 18:30:10.368955+03', '48196829-bc0b-47f3-bf5b-b44f8b71a8d9', 'WGNT-83ZP-FEU8');


--
-- Data for Name: invites; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public.invites VALUES ('248774891b9fb967', 'WGNT-83ZP-FEU8', 1, 1, 1, '48196829-bc0b-47f3-bf5b-b44f8b71a8d9', '2026-10-27 18:30:10.350688+03', NULL, false, '[{"username": "Fgt2", "accountId": "2623abf2-cc14-4352-bd69-21336ddd6160", "redeemedAtUtc": "2026-10-27T15:30:10.3595648Z"}]');


--
-- Data for Name: match_history; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public.match_history VALUES ('089caa17-d3f4-484d-9d48-d1e767d98931', '2026-10-27 18:08:45.052946+03', 'Online', '48196829-bc0b-47f3-bf5b-b44f8b71a8d9', 'c2ea4966-a250-47dc-92a4-146c16b0806d', 'Admin', 'Fgt1', 'NorthernRealms', 'NorthernRealms', 'Win', 2, 0, 1016, 15, '1df7d331-6573-4b55-9cd7-963a2c8fc6fe', NULL, '[]');
INSERT INTO public.match_history VALUES ('0b8ae1b6-86f2-41ec-b5a1-eb6c3a87557a', '2026-10-27 18:41:17.710806+03', 'Online', '2623abf2-cc14-4352-bd69-21336ddd6160', '48196829-bc0b-47f3-bf5b-b44f8b71a8d9', 'Fgt2', 'Admin', 'NorthernRealms', 'NorthernRealms', 'Loss', 0, 2, 986, -13, 'bc00da34-20cc-4b23-a4d6-096a945925e3', NULL, '[]');
INSERT INTO public.match_history VALUES ('8d2ad32a-58f1-4964-b857-1a14315e826c', '2026-10-27 18:41:17.710649+03', 'Online', '48196829-bc0b-47f3-bf5b-b44f8b71a8d9', '2623abf2-cc14-4352-bd69-21336ddd6160', 'Admin', 'Fgt2', 'NorthernRealms', 'NorthernRealms', 'Win', 2, 0, 1046, 13, 'bc00da34-20cc-4b23-a4d6-096a945925e3', NULL, '[]');
INSERT INTO public.match_history VALUES ('aaabc6eb-8cb1-4ead-8f0a-fe1ad0bd4b99', '2026-10-27 18:08:45.053363+03', 'Online', 'c2ea4966-a250-47dc-92a4-146c16b0806d', '48196829-bc0b-47f3-bf5b-b44f8b71a8d9', 'Fgt1', 'Admin', 'NorthernRealms', 'NorthernRealms', 'Loss', 0, 2, 985, -14, '1df7d331-6573-4b55-9cd7-963a2c8fc6fe', NULL, '[]');
INSERT INTO public.match_history VALUES ('ae5eddbf-5520-4d94-9d0e-47e35dd301f7', '2026-10-27 18:06:22.157627+03', 'Online', 'c2ea4966-a250-47dc-92a4-146c16b0806d', '48196829-bc0b-47f3-bf5b-b44f8b71a8d9', 'Fgt1', 'Admin', 'NorthernRealms', 'NorthernRealms', 'Loss', 0, 2, 1000, -15, '4421448d-31f4-42b7-b5e3-cc1deb8a6ed8', NULL, '[]');
INSERT INTO public.match_history VALUES ('af58d3aa-3c03-478f-b792-f96b7223991c', '2026-10-27 18:31:27.847747+03', 'Online', '48196829-bc0b-47f3-bf5b-b44f8b71a8d9', '2623abf2-cc14-4352-bd69-21336ddd6160', 'Admin', 'Fgt2', 'NorthernRealms', 'NorthernRealms', 'Win', 2, 0, 1031, 15, '5c7de1ca-cf0b-40d7-ba2b-060362e71ba0', NULL, '[]');
INSERT INTO public.match_history VALUES ('be157cab-fe69-466c-b08a-61418179a72d', '2026-10-27 18:31:27.847868+03', 'Online', '2623abf2-cc14-4352-bd69-21336ddd6160', '48196829-bc0b-47f3-bf5b-b44f8b71a8d9', 'Fgt2', 'Admin', 'NorthernRealms', 'NorthernRealms', 'Loss', 0, 2, 1000, -14, '5c7de1ca-cf0b-40d7-ba2b-060362e71ba0', NULL, '[]');
INSERT INTO public.match_history VALUES ('c5b85bf3-9479-49c3-9a90-e56a1b395f03', '2026-10-28 15:36:28.395863+03', 'Bot', '48196829-bc0b-47f3-bf5b-b44f8b71a8d9', NULL, 'Admin', 'Bot', 'NorthernRealms', 'Nilfgaard', 'Win', 2, 0, 1059, 0, '34824da8-74ed-452e-9ecf-9dba5d591dd8', 'Medium', '[]');
INSERT INTO public.match_history VALUES ('cbc92d9f-4235-411f-a13f-5377103af897', '2026-10-27 18:06:22.154552+03', 'Online', '48196829-bc0b-47f3-bf5b-b44f8b71a8d9', 'c2ea4966-a250-47dc-92a4-146c16b0806d', 'Admin', 'Fgt1', 'NorthernRealms', 'NorthernRealms', 'Win', 2, 0, 1000, 16, '4421448d-31f4-42b7-b5e3-cc1deb8a6ed8', NULL, '[]');


--
-- PostgreSQL database dump complete
--


